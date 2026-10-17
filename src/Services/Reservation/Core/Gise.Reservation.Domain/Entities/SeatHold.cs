using Gise.Reservation.Domain.Common;
using Gise.Reservation.Domain.Enums;

namespace Gise.Reservation.Domain.Entities;

// Bir sipariş için tutulan koltuk grubu. Süre dolunca koltuklar bırakılır.
public sealed class SeatHold : BaseEntity
{
    public Guid OrderId { get; private set; }
    public Guid EventId { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public HoldStatus Status { get; private set; }

    private SeatHold() { }

    public static SeatHold Create(Guid orderId, Guid eventId, DateTime expiresAt) => new()
    {
        OrderId = orderId,
        EventId = eventId,
        ExpiresAt = expiresAt,
        Status = HoldStatus.Active
    };
}
