using System.Text;
using Confluent.Kafka;
using Gise.Contracts.Events;
using Gise.Ticket.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gise.Ticket.InboxWorker;

// Catalog'un olaylarını Kafka'dan okuyup inbox'a yazar, sonra offset'i onaylar (bkz. tasarım 2.2). Inbox'ı aynı process'teki
// Worker işler. API'ler mesaj dinlemez; Telgraf'ta bu tüketici API'deydi.
public sealed class CatalogEventsConsumer : BackgroundService
{
    // Kapanma sinyalinin en geç ne kadar sürede fark edileceği; ayar değeri değil.
    private static readonly TimeSpan PollTimeout = TimeSpan.FromSeconds(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CatalogEventsConsumer> _logger;
    private readonly string _bootstrapServers;
    private readonly string _consumerGroup;
    private readonly TimeSpan _errorBackoff;

    public CatalogEventsConsumer(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<CatalogEventsConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        var kafka = configuration.GetRequiredSection("Kafka");
        _bootstrapServers = kafka["BootstrapServers"] ?? throw new InvalidOperationException("Kafka:BootstrapServers ayarı eksik.");
        _consumerGroup = kafka["ConsumerGroup"] ?? throw new InvalidOperationException("Kafka:ConsumerGroup ayarı eksik.");
        _errorBackoff = TimeSpan.FromSeconds(kafka.GetValue<int>("ErrorBackoffSeconds"));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Consume() bloklayan bir çağrı; uygulamanın açılışını tutmasın.
        await Task.Yield();

        using var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            GroupId = _consumerGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            // Offset yalnızca mesaj inbox'a yazıldıktan sonra onaylanır.
            EnableAutoCommit = false
        }).Build();

        consumer.Subscribe(Kafka.CatalogEventsTopic);
        _logger.LogInformation("Kafka tüketicisi başladı: {Topic}", Kafka.CatalogEventsTopic);

        // Topic yokluğu her turda yeniden loglanmasın.
        var topicMissingLogged = false;

        while (!stoppingToken.IsCancellationRequested)
        {
            ConsumeResult<string, string>? result = null;
            try
            {
                result = consumer.Consume(PollTimeout);
                if (result is null)
                    continue;

                await SaveToInboxAsync(result, stoppingToken);

                // Inbox'a yazıldıktan sonra: arada çökülürse mesaj yeniden gelir, inbox tekrarı yakalar.
                consumer.Commit(result);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (ConsumeException ex) when (ex.Error.Code == ErrorCode.UnknownTopicOrPart)
            {
                if (!topicMissingLogged)
                {
                    _logger.LogInformation("'{Topic}' topic'i henüz yok; Catalog ilk olayı gönderene kadar beklenecek.", Kafka.CatalogEventsTopic);
                    topicMissingLogged = true;
                }

                await Task.Delay(_errorBackoff, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kafka mesajı inbox'a yazılamadı.");

                // Okuma konumu bellekte ilerledi; geri sarılmazsa yazılamayan mesaj yeniden başlatılana kadar atlanırdı.
                if (result is not null)
                    consumer.Seek(result.TopicPartitionOffset);

                await Task.Delay(_errorBackoff, stoppingToken);
            }
        }

        consumer.Close();
    }

    private async Task SaveToInboxAsync(ConsumeResult<string, string> result, CancellationToken cancellationToken)
    {
        var headers = result.Message.Headers;
        // message-id başlığı olmayan mesaj için konumu tekil kimlik sayılır.
        var messageId = ReadHeader(headers, Kafka.MessageIdHeader)
            ?? $"{result.Topic}:{result.Partition.Value}:{result.Offset.Value}";

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InboxWorkerDbContext>();

        if (await db.InboxMessages.AnyAsync(m => m.MessageId == messageId, cancellationToken))
        {
            _logger.LogInformation("Tekrar gelen mesaj atlandı: {MessageId}", messageId);
            return;
        }

        db.InboxMessages.Add(new InboxMessage
        {
            MessageId = messageId,
            Topic = result.Topic,
            Type = ReadHeader(headers, Kafka.TypeHeader) ?? string.Empty,
            Key = result.Message.Key ?? string.Empty,
            Payload = result.Message.Value,
            TraceParent = ReadHeader(headers, Kafka.TraceParentHeader)
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    private static string? ReadHeader(Headers? headers, string name)
        => headers is not null && headers.TryGetLastBytes(name, out var bytes) ? Encoding.UTF8.GetString(bytes) : null;
}
