using Gise.Notification.Domain.Common;

namespace Gise.Notification.Domain.Entities;

// Kafka'dan gelen mesaj. MessageId tekil olduğu için aynı mesaj ikinci kez kaydedilemez;
// asıl iş ayrı bir işlemci tarafından yapılır.
public class InboxMessage : BaseEntity
{
    public string MessageId { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public string? TraceParent { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public int RetryCount { get; set; }
    public DateTime? NextAttemptAt { get; set; }
    public string? Error { get; set; }
    public bool IsDeadLettered { get; set; }
}
