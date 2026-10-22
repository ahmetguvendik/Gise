using MediatR;
using System.Text.Json.Serialization;

namespace Gise.Catalog.Application.Features.Commands.Event.Request;

// Admin onay bekleyen etkinliği sebebiyle taslağa geri gönderir; sebep organizatöre gösterilir.
public sealed class ReturnEventCommand : IRequest
{
    public string Reason { get; set; } = string.Empty;

    // Body'den gelmez — controller route'tan set eder.
    [JsonIgnore]
    public Guid EventId { get; set; }
}
