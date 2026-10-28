using Gise.Reservation.Domain.Common;
using Gise.Reservation.Domain.Enums;

namespace Gise.Reservation.Domain.Entities;

// Bir siparişin tuttuğu koltuklar. Kilidin kendisi Redis'te (değeri siparişin ID'si); bu kayıt hangi müşterinin hangi
// koltukları ne zamana kadar tuttuğunu saklar. Sipariş koltuklar tutulmadan kapandıysa koltuksuz "Released" kaydı olur.
public class SeatHold : BaseEntity
{
    public Guid OrderId { get; set; }
    public Guid EventId { get; set; }

    // Keycloak'taki müşteri (sub).
    public string CustomerId { get; set; } = string.Empty;

    public List<Guid> SeatIds { get; set; } = [];
    public DateTime ExpiresAt { get; set; }
    public HoldStatus Status { get; set; } = HoldStatus.Active;
}
