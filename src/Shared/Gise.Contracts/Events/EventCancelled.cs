namespace Gise.Contracts.Events;

// Catalog → catalog.events: etkinlik iptal edildi. Satış durur; satılmış biletler iade edilir.
public sealed class EventCancelled
{
    public Guid EventId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime CancelledAt { get; set; }
}
