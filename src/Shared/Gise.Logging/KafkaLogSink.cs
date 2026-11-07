using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Elastic.CommonSchema.Serilog;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;

namespace Gise.Logging;

// Asla exception fırlatmaz ve çağıranı bekletmez: Kafka çökünce istekler etkilenmemeli. Kendi hataları Serilog'un SelfLog'una
// yazılır; ILogger'a yazmak sonsuz döngü yaratırdı.
internal sealed class KafkaLogSink : IBatchedLogEventSink, IDisposable
{
    private readonly IProducer<Null, string> _producer;
    private readonly EcsTextFormatter _formatter = new();
    private readonly string _topic;
    private readonly string _auditTopic;

    public KafkaLogSink(string bootstrapServers, string topic, string auditTopic, int partitions, TimeSpan retention)
    {
        _topic = topic;
        _auditTopic = auditTopic;

        _producer = new ProducerBuilder<Null, string>(new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            // Kafka erişilemezken bellek şişmesin: kuyruk dolunca Produce hata verir, olay düşürülür.
            QueueBufferingMaxMessages = 100_000,
            MessageTimeoutMs = 10_000,
            // Log kaybı göze alınır; onay beklemek gönderimi yavaşlatırdı.
            Acks = Acks.None,
            LingerMs = 50,
            CompressionType = CompressionType.Lz4
        })
            .SetErrorHandler((_, e) => SelfLog.WriteLine("Kafka log sink hatası: {0}", e.Reason))
            // librdkafka kendi loglarını işleyici yoksa doğrudan stderr'e basıyor: Kafka kapalıyken saniyede onlarca satır.
            .SetLogHandler((_, message) => SelfLog.WriteLine("Kafka log sink: {0}", message.Message))
            .Build();

        // Kafka'da topic otomatik oluşturulmuyor (bkz. tasarım 3.5); her process açılışta dener, uygulamayı bekletmez.
        _ = EnsureTopicsAsync(bootstrapServers, partitions, retention);
    }

    private async Task EnsureTopicsAsync(string bootstrapServers, int partitions, TimeSpan retention)
    {
        try
        {
            using var admin = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = bootstrapServers })
                .SetLogHandler((_, message) => SelfLog.WriteLine("Kafka log sink: {0}", message.Message))
                .Build();
            await admin.CreateTopicsAsync(new[] { _topic, _auditTopic }.Distinct().Select(name => new TopicSpecification
            {
                Name = name,
                NumPartitions = partitions,
                ReplicationFactor = 1,
                // Logstash okuduktan sonra Kafka'da tutmaya gerek yok; kısa saklama.
                Configs = new Dictionary<string, string> { ["retention.ms"] = ((long)retention.TotalMilliseconds).ToString() }
            }));
        }
        catch (CreateTopicsException ex) when (ex.Results.All(r => r.Error.Code is ErrorCode.TopicAlreadyExists or ErrorCode.NoError))
        {
        }
        catch (Exception ex)
        {
            SelfLog.WriteLine("Log topic'i oluşturulamadı: {0}", ex.Message);
        }
    }

    public Task EmitBatchAsync(IReadOnlyCollection<LogEvent> batch)
    {
        foreach (var logEvent in batch)
        {
            try
            {
                var payload = new StringWriter();
                _formatter.Format(logEvent, payload);
                _producer.Produce(TopicFor(logEvent), new Message<Null, string> { Value = payload.ToString() });
            }
            catch (ProduceException<Null, string> ex)
            {
                SelfLog.WriteLine("Log olayı düşürüldü: {0}", ex.Error.Reason);
            }
            catch (Exception ex)
            {
                SelfLog.WriteLine("Log olayı biçimlendirilemedi: {0}", ex);
            }
        }

        return Task.CompletedTask;
    }

    // İnsanların yaptığı işlem (kullanıcısı olan denetim satırı, bkz. CommandLoggingBehavior) denetim topic'ine; worker'daki
    // sistem adımları ve diğer her şey process'in kendi topic'ine (bkz. tasarım 8).
    private string TopicFor(LogEvent logEvent) =>
        logEvent.Properties.TryGetValue("event.dataset", out var dataset)
        && dataset is ScalarValue { Value: "gise.audit" }
        && logEvent.Properties.ContainsKey("actor.id")
            ? _auditTopic
            : _topic;

    public void Dispose()
    {
        try
        {
            _producer.Flush(TimeSpan.FromSeconds(5));
        }
        catch (Exception ex)
        {
            SelfLog.WriteLine("Kafka log sink kapatılırken hata: {0}", ex);
        }
        finally
        {
            _producer.Dispose();
        }
    }
}
