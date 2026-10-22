using Gise.Reservation.Domain.Common;
using Gise.Reservation.Domain.Enums;

namespace Gise.Reservation.Domain.Entities;

// Bir koltuğun belirli bir etkinlikteki durumu. Etkinlik yayınlanınca (EventPublished) koltuklar Catalog'dan
// gRPC ile alınır, her biri için bir satır oluşur. Redis kilidi hızlı katman; kalıcı gerçek bu tablo.
public class EventSeat : BaseEntity
{
    public Guid EventId { get; set; }
    public Guid SeatId { get; set; }

    // Koltuk haritası ve satış Catalog'a sormadan çalışsın diye Catalog'dan kopyalanır.
    public Guid SectionId { get; set; }
    public string SectionName { get; set; } = string.Empty;
    public string Row { get; set; } = string.Empty;
    public int Number { get; set; }

    // Bölümün fiyatı; EventUpdated gelince Catalog'dan yeniden okunur.
    public decimal Price { get; set; }

    public SeatStatus Status { get; set; } = SeatStatus.Available;
    public Guid? HoldId { get; set; }

    // PostgreSQL xmin: aynı satırı aynı anda iki işlem değiştirirse ikincisi çakışma alır.
    public uint Version { get; set; }
}
