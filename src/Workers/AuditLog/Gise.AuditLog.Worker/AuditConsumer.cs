using Confluent.Kafka;
using Gise.HealthChecks;
using Microsoft.EntityFrameworkCore;

namespace Gise.AuditLog.Worker;

// Kafka'daki denetim satırlarını (logs.audit) denetim veritabanına yazar. Elasticsearch'e giden akış (Logstash) aynı topic'i
// kendi tüketici grubuyla okur; ikisi birbirini etkilemez. Güvence: offset yalnızca satırlar veritabanına yazılıp transaction
// onaylandıktan sonra onaylanır. Arada çökülürse mesajlar yeniden gelir; Kafka konumu tekil olduğu için ikinci kez yazılmaz
// (ON CONFLICT DO NOTHING). Yazılamayan parti geri sarılır ve yeniden denenir: hiçbir satır atlanmaz, hiçbiri atılmaz.
// Ayarlar appsettings'teki Kafka bölümünden.
public sealed class AuditConsumer : BackgroundService
{
    // Kapanma sinyalinin en geç ne kadar sürede fark edileceği; ayar değeri değil.
    private static readonly TimeSpan PollTimeout = TimeSpan.FromSeconds(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly WorkerHeartbeat _heartbeat;
    private readonly ILogger<AuditConsumer> _logger;
    private readonly string _bootstrapServers;
    private readonly string _topic;
    private readonly string _consumerGroup;
    private readonly int _batchSize;
    private readonly TimeSpan _errorBackoff;

    public AuditConsumer(IServiceScopeFactory scopeFactory, IConfiguration configuration, WorkerHeartbeat heartbeat, ILogger<AuditConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _heartbeat = heartbeat;
        _logger = logger;

        var kafka = configuration.GetRequiredSection("Kafka");
        _bootstrapServers = kafka["BootstrapServers"] ?? throw new InvalidOperationException("Kafka:BootstrapServers ayarı eksik.");
        _topic = kafka["Topic"] ?? throw new InvalidOperationException("Kafka:Topic ayarı eksik.");
        _consumerGroup = kafka["ConsumerGroup"] ?? throw new InvalidOperationException("Kafka:ConsumerGroup ayarı eksik.");
        _batchSize = kafka.GetValue<int>("BatchSize");
        _errorBackoff = TimeSpan.FromSeconds(kafka.GetValue<int>("ErrorBackoffSeconds"));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Consume() bloklayan bir çağrı; uygulamanın açılışını tutmasın.
        await Task.Yield();

        using var consumer = new ConsumerBuilder<Ignore, string>(new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            GroupId = _consumerGroup,
            // İlk bağlanışta topic'in başından: worker kurulmadan önce birikmiş denetim satırları da yazılır.
            AutoOffsetReset = AutoOffsetReset.Earliest,
            // Offset yalnızca satırlar veritabanına yazıldıktan sonra onaylanır.
            EnableAutoCommit = false
        }).Build();

        consumer.Subscribe(_topic);
        _logger.LogInformation("Denetim tüketicisi başladı: {Topic}", _topic);

        // Topic yokluğu her turda yeniden loglanmasın.
        var topicMissingLogged = false;

        while (!stoppingToken.IsCancellationRequested)
        {
            var batch = new List<ConsumeResult<Ignore, string>>();
            try
            {
                Read(consumer, batch);
                if (batch.Count > 0)
                {
                    await StoreAsync(batch, stoppingToken);
                    Commit(consumer, batch);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (ConsumeException ex) when (ex.Error.Code == ErrorCode.UnknownTopicOrPart)
            {
                if (!topicMissingLogged)
                {
                    _logger.LogInformation("'{Topic}' topic'i henüz yok; ilk denetim satırını yazan servis açana kadar beklenecek.", _topic);
                    topicMissingLogged = true;
                }

                await Task.Delay(_errorBackoff, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Denetim satırları veritabanına yazılamadı; {Count} satır yeniden denenecek.", batch.Count);

                // Okuma konumu bellekte ilerledi; geri sarılmazsa yazılamayan satırlar yeniden başlatılana kadar atlanırdı.
                Rewind(consumer, batch);

                await Task.Delay(_errorBackoff, stoppingToken);
            }
            finally
            {
                // Boşta da her turda: asılı kalan döngü bu satıra ulaşamaz ve sağlık kontrolü bunu yakalar.
                _heartbeat.Beat();
            }
        }

        consumer.Close();
    }

    // İlk mesaj için bekler, sonra beklemeden hazır olanları alır; bir parti tek transaction'da yazılır.
    private void Read(IConsumer<Ignore, string> consumer, List<ConsumeResult<Ignore, string>> batch)
    {
        var first = consumer.Consume(PollTimeout);
        if (first is null)
            return;

        batch.Add(first);
        while (batch.Count < _batchSize && consumer.Consume(TimeSpan.Zero) is { } next)
            batch.Add(next);
    }

    private async Task StoreAsync(List<ConsumeResult<Ignore, string>> batch, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuditDbContext>();

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var written = 0;
        foreach (var result in batch)
        {
            var record = AuditRecordMapper.Map(
                result.Topic, result.Partition.Value, result.Offset.Value, result.Message.Timestamp.UtcDateTime, result.Message.Value);
            written += await InsertAsync(db, record, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        // Debug: her partide yazılırdı; tekrar gelenler (written < batch) yeniden başlatma sonrası beklenir, hata değil.
        _logger.LogDebug("Denetim: {Written}/{Count} satır yazıldı.", written, batch.Count);
    }

    // Aynı Kafka mesajı ikinci kez gelirse satır eklenmez (0 döner).
    private static Task<int> InsertAsync(AuditDbContext db, AuditRecord r, CancellationToken cancellationToken)
        => db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO audit_records
                (kafka_topic, kafka_partition, kafka_offset, occurred_at, logged_at, service, actor_id, actor_roles, action,
                 outcome, error_code, status_code, client_ip, trace_id, targets, message, payload, created_at)
            VALUES
                ({r.KafkaTopic}, {r.KafkaPartition}, {r.KafkaOffset}, {r.OccurredAt}, {r.LoggedAt}, {r.Service}, {r.ActorId},
                 {r.ActorRoles}, {r.Action}, {r.Outcome}, {r.ErrorCode}, {r.StatusCode}, {r.ClientIp}, {r.TraceId},
                 {r.Targets}::jsonb, {r.Message}, {r.Payload}::jsonb, {DateTime.UtcNow})
            ON CONFLICT (kafka_topic, kafka_partition, kafka_offset) DO NOTHING
            """, cancellationToken);

    // Her bölümün son offset'inden sonrası onaylanır.
    private static void Commit(IConsumer<Ignore, string> consumer, List<ConsumeResult<Ignore, string>> batch)
        => consumer.Commit(batch
            .GroupBy(r => r.TopicPartition)
            .Select(g => new TopicPartitionOffset(g.Key, g.Max(r => r.Offset.Value) + 1)));

    // Her bölüm, partideki ilk (en küçük) offset'e geri sarılır.
    private static void Rewind(IConsumer<Ignore, string> consumer, List<ConsumeResult<Ignore, string>> batch)
    {
        foreach (var partition in batch.GroupBy(r => r.TopicPartition))
            consumer.Seek(new TopicPartitionOffset(partition.Key, partition.Min(r => r.Offset.Value)));
    }
}
