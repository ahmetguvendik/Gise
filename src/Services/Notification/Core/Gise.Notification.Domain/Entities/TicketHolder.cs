using Gise.Notification.Domain.Common;

namespace Gise.Notification.Domain.Entities;

// Bir siparişin bilet sahibi; Id = siparişin ID'si. Kafka'daki TicketsIssued'dan yazılır. Etkinlik iptal edilince ya da
// tarihi değişince kime e-posta gideceği buradan bulunur: Catalog bilet sahiplerini bilmez.
public class TicketHolder : BaseEntity
{
    public Guid EventId { get; set; }
    public string CustomerId { get; set; } = string.Empty;

    // Sipariş açılırken token'dan alınan adres ve ad; olaylarla gelir (bkz. tasarım 3.9).
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    // E-postada yazılır; etkinlik güncellenince (EventUpdated) yenilenir.
    public string EventTitle { get; set; } = string.Empty;
    public DateTime EventStartsAt { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public int TicketCount { get; set; }
}
