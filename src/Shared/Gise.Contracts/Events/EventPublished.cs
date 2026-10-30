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
}
