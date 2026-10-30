namespace Gise.Search.Domain.Documents;

// Elasticsearch'teki arama kaydı. search-sync-worker Catalog'dan gRPC ile düzenli olarak baştan kurar; yalnızca yayındaki,
// henüz başlamamış etkinlikler bulunur.
public sealed class EventDocument
{
    // Catalog'daki etkinliğin ID'si; Elasticsearch'te de kaydın kimliği.
    public Guid Id { get; set; }
    public Guid VenueId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string VenueName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;

    // UTC.
    public DateTime StartsAt { get; set; }
    public decimal MinPrice { get; set; }

    // MinIO'daki afişin temel anahtarı; adresler sonuç dönerken üretilir.
    public string? PosterKey { get; set; }

    // Reservation'dan: koltuk sayısı ve satılmamış koltuk sayısı. Stoğu henüz kurulmamış etkinlikte boş.
    public int? SeatCount { get; set; }
    public int? AvailableCount { get; set; }
}
