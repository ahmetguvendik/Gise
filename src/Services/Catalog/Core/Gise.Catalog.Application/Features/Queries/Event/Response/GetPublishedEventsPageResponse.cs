namespace Gise.Catalog.Application.Features.Queries.Event.Response;

public sealed class GetPublishedEventsPageResponse
{
    public List<PublishedEventResponse> Events { get; set; } = [];
    public bool HasMore { get; set; }
}

// Search'ün arama kaydı için gereken alanlar; EventPublished ile aynı.
public sealed class PublishedEventResponse
{
    public Guid EventId { get; set; }
    public Guid VenueId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public decimal MinPrice { get; set; }
    public string? PosterKey { get; set; }
}
