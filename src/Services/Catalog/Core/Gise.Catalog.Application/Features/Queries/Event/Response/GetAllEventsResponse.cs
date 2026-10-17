namespace Gise.Catalog.Application.Features.Queries.Event.Response;

public sealed class GetAllEventsResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid VenueId { get; set; }
    public DateTime StartsAt { get; set; }
}
