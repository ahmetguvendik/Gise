namespace Gise.Catalog.Application.Features.Queries.Event.Response;

public sealed class GetAllEventsResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Venue { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; }
}
