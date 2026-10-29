using Gise.Ticket.Domain.Common;

namespace Gise.Ticket.Domain.Entities;

// İptal edilen etkinlik; Id = Catalog'daki etkinliğin ID'si. Kafka'dan EventCancelled gelince yazılır: o andaki biletler
// iptal olur, ödemesi o sırada süren bir sipariş için sonradan gelen IssueTickets bilet çıkarmaz.
public class CancelledEvent : BaseEntity
{
    public DateTime CancelledAt { get; set; }
}
