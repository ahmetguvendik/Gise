using Gise.Reservation.Domain.Common;
using Gise.Reservation.Domain.Enums;

namespace Gise.Reservation.Domain.Entities;

// Bir koltuğun belirli bir etkinlikteki durumu. Etkinlik yayınlanınca (EventPublished) mekânın her
// koltuğu için bir satır oluşur. Redis kilidi hızlı katman; kalıcı gerçek bu tablo.
public class EventSeat : BaseEntity
{
    public Guid EventId { get; set; }
    public Guid SeatId { get; set; }
    public SeatStatus Status { get; set; } = SeatStatus.Available;
    public Guid? HoldId { get; set; }

    // PostgreSQL xmin: aynı satırı aynı anda iki işlem değiştirirse ikincisi çakışma alır.
    public uint Version { get; set; }
}
