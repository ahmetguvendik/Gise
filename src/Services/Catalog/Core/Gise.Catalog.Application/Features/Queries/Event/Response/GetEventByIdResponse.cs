namespace Gise.Catalog.Application.Features.Queries.Event.Response;

public sealed class GetEventByIdResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid VenueId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; }
    public string OrganizerId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
