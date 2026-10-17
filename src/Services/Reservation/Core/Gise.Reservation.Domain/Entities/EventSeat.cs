using Gise.Reservation.Domain.Common;
using Gise.Reservation.Domain.Enums;

namespace Gise.Reservation.Domain.Entities;

// Bir koltuğun belirli bir etkinlikteki durumu. Etkinlik yayınlanınca (EventPublished) mekânın her
// koltuğu için bir satır oluşur. Redis kilidi hızlı katman; kalıcı gerçek bu tablo.
public sealed class EventSeat : BaseEntity
{
    public Guid EventId { get; private set; }
    public Guid SeatId { get; private set; }
    public SeatStatus Status { get; private set; }
    public Guid? HoldId { get; private set; }

    // PostgreSQL xmin: aynı satırı aynı anda iki işlem değiştirirse ikincisi çakışma alır.
    public uint Version { get; private set; }

    private EventSeat() { }

    public static EventSeat Create(Guid eventId, Guid seatId) => new()
    {
        EventId = eventId,
        SeatId = seatId,
        Status = SeatStatus.Available
    };
}
