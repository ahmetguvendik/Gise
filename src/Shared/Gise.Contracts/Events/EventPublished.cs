namespace Gise.Contracts.Events;

// Catalog → catalog.events: etkinlik satışa açıldı. Koltuklar olayda yok; isteyen servis
// Catalog'un gRPC ucundan (GetEventSeats) alır.
public sealed class EventPublished
{
    public Guid EventId { get; set; }
    public Guid VenueId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public decimal MinPrice { get; set; }

    // MinIO'daki afişin temel anahtarı (ör. "posters/2026/10/<id>"); afiş yoksa null. Boyların adresini dinleyen servis
    // kendisi üretir (Search sonuçlarında afiş gösterir).
    public string? PosterKey { get; set; }
}
