using Gise.Catalog.Application.Features.Commands.Event.Response;
using MediatR;
using System.Text.Json.Serialization;

namespace Gise.Catalog.Application.Features.Commands.Event.Request;

// Ad ve tarih her zaman (başlamadan önce), mekân yalnızca taslakta değişir.
public sealed class UpdateEventCommand : IRequest<UpdateEventResponse>
{
    public string Title { get; set; } = string.Empty;
    public Guid VenueId { get; set; }
    public DateTimeOffset StartsAt { get; set; }

    // Body'den gelmez — controller route'tan ve JWT'den set eder.
    [JsonIgnore]
    public Guid EventId { get; set; }

    [JsonIgnore]
    public string UserId { get; set; } = string.Empty;

    [JsonIgnore]
    public bool IsAdmin { get; set; }
}
