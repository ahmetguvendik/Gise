namespace Gise.Catalog.Application.Features.Queries.Event.Response;

public sealed class GetMyEventsResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; }

    // Afiş yoksa boş; ön yüz o zaman renkli yer tutucuyu çizer.
    public string? PosterUrl { get; set; }
    public string? PosterSrcset { get; set; }
    public Guid VenueId { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public decimal? MinPrice { get; set; }
}
