using Confluent.Kafka;
using Elastic.CommonSchema.Serilog;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;

namespace Gise.Logging;

// İnsanların yaptığı işlemlerin (denetim satırları) Kafka'ya gidişi. Diğer loglardan ayrı, çünkü kayıp göze alınmıyor
// (bkz. tasarım 8): onaylı yazma (acks=all, idempotent üretici), Serilog'un 10.000'lik toplu kuyruğundan geçmez (kuyruk
// dolunca olay düşerdi), iletim uzun denenir (2 dk). Başarısız iletim sessiz kalmaz: satır stderr'e tam metniyle yazılır
// (konsol sink'i aynı satırı stdout'a zaten yazıyor; kayıt konteyner loglarından kurtarılır). İşlemi bekletmez: Produce
// librdkafka'nın kendi kuyruğuna ekler. Kalan boşluk: satır Kafka'ya gitmeden process çökerse kaybolur (en fazla birkaç ms).
internal sealed class AuditKafkaSink : ILogEventSink, IDisposable
{
    private readonly IProducer<Null, string> _producer;
    private readonly EcsTextFormatter _formatter = new();
    private readonly string _topic;

    public AuditKafkaSink(string bootstrapServers, string topic)
    {
        _topic = topic;

        _producer = new ProducerBuilder<Null, string>(new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            Acks = Acks.All,
            // Yeniden denemede aynı satır iki kez yazılmaz, sıra bozulmaz.
            EnableIdempotence = true,
            MessageTimeoutMs = 120_000,
            QueueBufferingMaxMessages = 100_000,
            LingerMs = 5,
            CompressionType = CompressionType.Lz4
        })
            .SetErrorHandler((_, e) => SelfLog.WriteLine("Denetim Kafka üreticisi hatası: {0}", e.Reason))
            .SetLogHandler((_, message) => SelfLog.WriteLine("Denetim Kafka üreticisi: {0}", message.Message))
            .Build();
    }

    public void Emit(LogEvent logEvent)
    {
        string payload;
        try
        {
            var writer = new StringWriter();
            _formatter.Format(logEvent, writer);
            payload = writer.ToString();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"DENETIM satırı biçimlendirilemedi: {ex.Message}");
            return;
        }

        try
        {
            _producer.Produce(_topic, new Message<Null, string> { Value = payload }, OnDelivered);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"DENETIM satırı Kafka kuyruğuna eklenemedi ({ex.Message}): {payload}");
        }
    }

    private static void OnDelivered(DeliveryReport<Null, string> report)
    {
        if (report.Error.IsError)
            Console.Error.WriteLine($"DENETIM satırı Kafka'ya iletilemedi ({report.Error.Reason}): {report.Message.Value}");
    }

    public void Dispose()
    {
        try
        {
            // Kapanışta kuyrukta bekleyen denetim satırları gönderilsin.
            _producer.Flush(TimeSpan.FromSeconds(10));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"DENETIM Kafka üreticisi kapatılırken hata: {ex.Message}");
        }
        finally
        {
            _producer.Dispose();
        }
    }
}
