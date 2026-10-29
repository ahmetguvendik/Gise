namespace Gise.Catalog.Application.Features.Queries.Event.Response;

public sealed class GetEventSummaryResponse
{
    public Guid EventId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
}
