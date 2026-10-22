using Gise.Reservation.Domain.Common;

namespace Gise.Reservation.Domain.Entities;

// Satışa açılan etkinlik. Catalog'dan EventPublished gelince oluşur; Id, Catalog'daki etkinliğin ID'sidir.
public class EventStock : BaseEntity
{
    public DateTime StartsAt { get; set; }

    // EventCancelled gelince dolar; satış durur.
    public DateTime? CancelledAt { get; set; }

    public ICollection<EventSeat> Seats { get; set; } = [];
}
