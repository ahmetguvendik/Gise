using Gise.Reservation.Domain.Common;
using Gise.Reservation.Domain.Enums;

namespace Gise.Reservation.Domain.Entities;

// Bir sipariş için tutulan koltuk grubu. Süre dolunca koltuklar bırakılır.
public class SeatHold : BaseEntity
{
    public Guid OrderId { get; set; }
    public Guid EventId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public HoldStatus Status { get; set; } = HoldStatus.Active;
}
