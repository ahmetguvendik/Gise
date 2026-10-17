using Gise.Catalog.Application.Features.Commands.Event.Response;
using MediatR;
using System.Text.Json.Serialization;

namespace Gise.Catalog.Application.Features.Commands.Event.Request;

public sealed class PublishEventCommand : IRequest<PublishEventResponse>
{
    // Body'den gelmez — controller route'tan ve JWT'den set eder.
    [JsonIgnore]
    public Guid EventId { get; set; }

    [JsonIgnore]
    public string UserId { get; set; } = string.Empty;

    [JsonIgnore]
    public bool IsAdmin { get; set; }
}
