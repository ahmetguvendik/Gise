namespace Gise.Search.Domain.Documents;

// Elasticsearch'teki arama kaydı; EF entity'si değil. Catalog'un olaylarından (inbox üzerinden) beslenir, ilk dizin
// Catalog'dan gRPC ile kurulur. Yalnızca yayındaki etkinlikler bulunur; iptal edilen silinir.
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
}
