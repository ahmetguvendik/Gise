namespace Gise.Catalog.Application.Features.Commands.Event.Response;

public sealed class UpdateEventResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid VenueId { get; set; }
    public DateTime StartsAt { get; set; }
    public string Status { get; set; } = string.Empty;
}
