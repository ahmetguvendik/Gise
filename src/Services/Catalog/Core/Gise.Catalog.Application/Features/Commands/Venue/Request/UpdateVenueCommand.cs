using Gise.Catalog.Application.Features.Commands.Venue.Response;
using MediatR;
using System.Text.Json.Serialization;

namespace Gise.Catalog.Application.Features.Commands.Venue.Request;

// Ad ve şehir her zaman değişebilir; bölüm düzeni ayrı komutla (UpdateVenueLayoutCommand).
public sealed class UpdateVenueCommand : IRequest<UpdateVenueResponse>
{
    // Body'den gelmez — controller route'tan set eder.
    [JsonIgnore]
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
}
