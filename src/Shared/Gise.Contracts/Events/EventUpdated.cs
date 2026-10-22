namespace Gise.Contracts.Events;

// Catalog → catalog.events: yayındaki etkinliğin adı, tarihi ya da fiyatları değişti. Olay etkinliğin
// güncel hâlini taşır; dinleyen servis kendi kopyasını bununla değiştirir.
public sealed class EventUpdated
{
    public Guid EventId { get; set; }
    public Guid VenueId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public decimal MinPrice { get; set; }

    // Değişiklikten önceki tarih; StartsAt'ten farklıysa etkinlik ertelenmiş demektir (bilet sahiplerine bildirim).
    public DateTime PreviousStartsAt { get; set; }
}
