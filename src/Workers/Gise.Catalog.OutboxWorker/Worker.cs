using System.Text;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Gise.Catalog.Domain.Entities;
using Gise.Contracts.Events;
using Gise.HealthChecks;
using Microsoft.EntityFrameworkCore;

namespace Gise.Catalog.OutboxWorker;

// Catalog'un outbox'ında bekleyen olayları Kafka'ya gönderir (bkz. tasarım 2.2). Ayarlar appsettings'teki
// Kafka ve Outbox bölümlerinden okunur.
public sealed class Worker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<Worker> _logger;
    private readonly WorkerHeartbeat _heartbeat;
    private readonly string _bootstrapServers;
    private readonly int _topicPartitions;
    private readonly short _topicReplicationFactor;
    private readonly int _batchSize;
    private readonly TimeSpan _pollInterval;
    private readonly int _maxRetryCount;
    private readonly int _produceTimeoutMs;
    private readonly TimeSpan _retention;
    private readonly TimeSpan _cleanupInterval;

    private DateTime _lastCleanupAt = DateTime.MinValue;

    public Worker(IServiceScopeFactory scopeFactory, ILogger<Worker> logger, IConfiguration configuration, WorkerHeartbeat heartbeat)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _heartbeat = heartbeat;

        var kafka = configuration.GetRequiredSection("Kafka");
        _bootstrapServers = kafka["BootstrapServers"] ?? throw new InvalidOperationException("Kafka:BootstrapServers ayarı eksik.");
        _topicPartitions = kafka.GetValue<int>("TopicPartitions");
        _topicReplicationFactor = kafka.GetValue<short>("TopicReplicationFactor");

        var outbox = configuration.GetRequiredSection("Outbox");
        _batchSize = outbox.GetValue<int>("BatchSize");
        _pollInterval = TimeSpan.FromSeconds(outbox.GetValue<int>("PollIntervalSeconds"));
        _maxRetryCount = outbox.GetValue<int>("MaxRetryCount");
        _produceTimeoutMs = outbox.GetValue<int>("ProduceTimeoutSeconds") * 1000;
        _retention = TimeSpan.FromDays(outbox.GetValue<int>("RetentionDays"));
        _cleanupInterval = TimeSpan.FromMinutes(outbox.GetValue<int>("CleanupIntervalMinutes"));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = _bootstrapServers,
            // Yeniden denemede aynı mesaj iki kez yazılmaz, bölüm içindeki sıra korunur.
            EnableIdempotence = true,
            // librdkafka varsayılanı 5 dk: Kafka çökünce tur tek mesajda asılı kalmasın.
            MessageTimeoutMs = _produceTimeoutMs
        }).Build();

        var topicsReady = false;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!topicsReady)
                {
                    await CreateTopicsAsync();
                    topicsReady = true;
                }

                await PublishPendingAsync(producer, stoppingToken);
                await CleanupAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                // Tur başarısız oldu (Kafka ya da veritabanı yok); bir sonraki turda tekrar denenir.
                _logger.LogError(ex, "Outbox turu tamamlanamadı.");
            }

            // try/catch'in dışında: hata alıp dönmeye devam eden döngü canlıdır; asılı kalan döngü
            // bu satıra hiç ulaşamaz ve sağlık kontrolü bunu yakalar.
            _heartbeat.Beat();

            await Task.Delay(_pollInterval, stoppingToken);
        }
    }

    // Kafka'da otomatik topic oluşturma kapalı; olayı yayan servis kendi topic'ini açılışta açar.
    private async Task CreateTopicsAsync()
    {
        using var admin = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = _bootstrapServers }).Build();

        try
        {
            await admin.CreateTopicsAsync(
            [
                new TopicSpecification
                {
                    Name = Kafka.CatalogEventsTopic,
                    NumPartitions = _topicPartitions,
                    ReplicationFactor = _topicReplicationFactor
                }
            ]);

            _logger.LogInformation("Kafka topic'i oluşturuldu: {Topic}", Kafka.CatalogEventsTopic);
        }
        catch (CreateTopicsException ex) when (ex.Results.All(r => r.Error.Code is ErrorCode.NoError or ErrorCode.TopicAlreadyExists))
        {
            // Topic zaten var.
        }
    }

    private async Task PublishPendingAsync(IProducer<string, string> producer, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OutboxWorkerDbContext>();

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // SKIP LOCKED: servisin birden çok kopyası çalışsa da aynı satırı ikisi birden göndermez.
        // NOT EXISTS: aynı anahtarın (etkinliğin) daha eski bir olayı beklerken yenisi onu geçmez;
        // yoksa "iptal edildi", "yayınlandı"dan önce gidebilirdi.
        var messages = await db.OutboxMessages
            .FromSql($"""
                SELECT m.* FROM outbox_messages m
                WHERE m.processed_at IS NULL
                  AND NOT m.is_dead_lettered
                  AND (m.next_attempt_at IS NULL OR m.next_attempt_at <= now())
                  AND NOT EXISTS (
                      SELECT 1 FROM outbox_messages earlier
                      WHERE earlier.key = m.key
                        AND earlier.processed_at IS NULL
                        AND NOT earlier.is_dead_lettered
                        AND earlier.created_at < m.created_at)
                ORDER BY m.created_at
                LIMIT {_batchSize}
                FOR UPDATE OF m SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0)
            return;

        foreach (var message in messages)
        {
            try
            {
                await producer.ProduceAsync(message.Topic, ToKafkaMessage(message), cancellationToken);

                message.ProcessedAt = DateTime.UtcNow;
                message.Error = null;
            }
            catch (ProduceException<string, string> ex)
            {
                message.RetryCount++;
                message.Error = ex.Error.Reason;

                if (message.RetryCount >= _maxRetryCount)
                {
                    message.IsDeadLettered = true;
                    _logger.LogError("Outbox mesajı {Id} ({Type}) {RetryCount} denemede gönderilemedi, dead-letter olarak işaretlendi: {Error}",
                        message.Id, message.Type, message.RetryCount, message.Error);
                }
                else
                {
                    // Artan bekleme: 2, 4, 8 … saniye, en fazla 5 dakika.
                    message.NextAttemptAt = DateTime.UtcNow.AddSeconds(Math.Min(Math.Pow(2, message.RetryCount), 300));
                    _logger.LogWarning("Outbox mesajı {Id} ({Type}) gönderilemedi, {RetryCount}. deneme {NextAttemptAt} sonrasında: {Error}",
                        message.Id, message.Type, message.RetryCount, message.NextAttemptAt, message.Error);
                }

                // Kafka'ya ulaşılamıyorsa turun geri kalanı da başarısız olur; kalanlar sonraki tura kalır.
                break;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var sent = messages.Count(m => m.ProcessedAt is not null);
        if (sent > 0)
            _logger.LogInformation("Outbox: {Count} olay Kafka'ya gönderildi.", sent);
    }

    private static Message<string, string> ToKafkaMessage(OutboxMessage message)
    {
        var kafkaMessage = new Message<string, string>
        {
            Key = message.Key,
            Value = message.Payload,
            Headers = new Headers
            {
                { Kafka.TypeHeader, Encoding.UTF8.GetBytes(message.Type) },
                { Kafka.MessageIdHeader, Encoding.UTF8.GetBytes(message.Id.ToString()) }
            }
        };

        if (message.TraceParent is { Length: > 0 } traceParent)
            kafkaMessage.Headers.Add(Kafka.TraceParentHeader, Encoding.UTF8.GetBytes(traceParent));

        return kafkaMessage;
    }

    // Gönderilmiş satırlar bir süre sonra silinir; tablo sonsuza kadar büyümez.
    private async Task CleanupAsync(CancellationToken cancellationToken)
    {
        if (DateTime.UtcNow - _lastCleanupAt < _cleanupInterval)
            return;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OutboxWorkerDbContext>();

        var threshold = DateTime.UtcNow - _retention;
        var deleted = await db.OutboxMessages
            .Where(m => m.ProcessedAt != null && m.ProcessedAt < threshold)
            .ExecuteDeleteAsync(cancellationToken);

        _lastCleanupAt = DateTime.UtcNow;

        if (deleted > 0)
            _logger.LogInformation("Outbox: {Count} eski satır temizlendi.", deleted);
    }
}
