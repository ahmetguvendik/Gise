using Gise.Catalog.Application.Features.Commands.Event.Response;
using MediatR;
using System.Text.Json.Serialization;

namespace Gise.Catalog.Application.Features.Commands.Event.Request;

// Yalnızca admin. Geri alınamaz. Sebep etkinlik sayfasında gösterilir, bilet sahiplerine iletilir.
public sealed class CancelEventCommand : IRequest<CancelEventResponse>
{
    public string Reason { get; set; } = string.Empty;

    // Body'den gelmez — controller route'tan set eder.
    [JsonIgnore]
    public Guid EventId { get; set; }
}
