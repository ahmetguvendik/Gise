using Gise.Order.Domain.Common;

namespace Gise.Order.Domain.Entities;

// Kafka'ya gidecek olay. İş verisiyle aynı transaction'da yazılır; ayrı bir worker sonra gönderir.
public sealed class OutboxMessage : BaseEntity
{
    public string Topic { get; private set; } = string.Empty;

    // Kafka bölüm anahtarı: aynı anahtarlı olaylar aynı bölüme düşer ve sırası korunur.
    public string Key { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public string? TraceParent { get; private set; }

    public DateTime? ProcessedAt { get; private set; }
    public int RetryCount { get; private set; }
    public DateTime? NextAttemptAt { get; private set; }
    public string? Error { get; private set; }
    public bool IsDeadLettered { get; private set; }

    private OutboxMessage() { }

    public static OutboxMessage Create(string topic, string key, string payload, string? traceParent) => new()
    {
        Topic = topic,
        Key = key,
        Payload = payload,
        TraceParent = traceParent
    };
}
