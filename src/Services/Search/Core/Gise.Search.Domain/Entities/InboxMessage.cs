using Gise.Search.Domain.Common;

namespace Gise.Search.Domain.Entities;

// Kafka'dan gelen mesaj. MessageId tekil olduğu için aynı mesaj ikinci kez kaydedilemez;
// asıl iş ayrı bir işlemci tarafından yapılır.
public sealed class InboxMessage : BaseEntity
{
    public string MessageId { get; private set; } = string.Empty;
    public string Topic { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public string? TraceParent { get; private set; }

    public DateTime? ProcessedAt { get; private set; }
    public int RetryCount { get; private set; }
    public DateTime? NextAttemptAt { get; private set; }
    public string? Error { get; private set; }
    public bool IsDeadLettered { get; private set; }

    private InboxMessage() { }

    public static InboxMessage Create(string messageId, string topic, string payload, string? traceParent) => new()
    {
        MessageId = messageId,
        Topic = topic,
        Payload = payload,
        TraceParent = traceParent
    };
}
