using Gise.Catalog.Domain.Common;

namespace Gise.Catalog.Domain.Entities;

// Kafka'ya gidecek olay. İş verisiyle aynı transaction'da yazılır; ayrı bir worker sonra gönderir.
public class OutboxMessage : BaseEntity
{
    public string Topic { get; set; } = string.Empty;

    // Kafka bölüm anahtarı: aynı anahtarlı olaylar aynı bölüme düşer ve sırası korunur.
    public string Key { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public string? TraceParent { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public int RetryCount { get; set; }
    public DateTime? NextAttemptAt { get; set; }
    public string? Error { get; set; }
    public bool IsDeadLettered { get; set; }
}
