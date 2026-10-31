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

    public KafkaLogSink(string bootstrapServers, string topic, int partitions, TimeSpan retention)
    {
        _topic = topic;

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
            .Build();

        // Kafka'da topic otomatik oluşturulmuyor (bkz. tasarım 3.5); her process açılışta dener, uygulamayı bekletmez.
        _ = EnsureTopicAsync(bootstrapServers, partitions, retention);
    }

    private async Task EnsureTopicAsync(string bootstrapServers, int partitions, TimeSpan retention)
    {
        try
        {
            using var admin = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = bootstrapServers }).Build();
            await admin.CreateTopicsAsync([
                new TopicSpecification
                {
                    Name = _topic,
                    NumPartitions = partitions,
                    ReplicationFactor = 1,
                    // Logstash okuduktan sonra Kafka'da tutmaya gerek yok; kısa saklama.
                    Configs = new Dictionary<string, string> { ["retention.ms"] = ((long)retention.TotalMilliseconds).ToString() }
                }
            ]);
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
                _producer.Produce(_topic, new Message<Null, string> { Value = payload.ToString() });
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
