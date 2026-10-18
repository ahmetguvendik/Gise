using Gise.Catalog.Application.Features.Commands.Venue.Response;
using MediatR;
using System.Text.Json.Serialization;

namespace Gise.Catalog.Application.Features.Commands.Venue.Request;

// Bölüm düzeni bütün olarak değişir; yalnızca mekânda hiç etkinlik yokken.
public sealed class UpdateVenueLayoutCommand : IRequest<UpdateVenueLayoutResponse>
{
    // Body'den gelmez — controller route'tan set eder.
    [JsonIgnore]
    public Guid Id { get; set; }

    public List<VenueSectionInput> Sections { get; set; } = [];
}
