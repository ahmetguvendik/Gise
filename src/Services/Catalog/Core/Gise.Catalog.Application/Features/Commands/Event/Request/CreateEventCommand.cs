using System.Text.Json.Serialization;

namespace Gise.Catalog.Application.Features.Commands.Event.Request;

public sealed class CreateEventCommand
{
    public string Title { get; set; } = string.Empty;
    public Guid VenueId { get; set; }
    public DateTimeOffset StartsAt { get; set; }

    // Body'den gelmez — controller JWT'deki sub'dan set eder.
    [JsonIgnore]
    public string OrganizerId { get; set; } = string.Empty;
}
