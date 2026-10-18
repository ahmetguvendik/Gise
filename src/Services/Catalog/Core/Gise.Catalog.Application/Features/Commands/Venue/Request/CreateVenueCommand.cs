using Gise.Catalog.Application.Features.Commands.Venue.Response;
using MediatR;

namespace Gise.Catalog.Application.Features.Commands.Venue.Request;

// Mekân, bölümleri ve sıralarıyla tek istekte oluşturulur; koltuklar sıra tanımından üretilir.
public sealed class CreateVenueCommand : IRequest<CreateVenueResponse>
{
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public List<VenueSectionInput> Sections { get; set; } = [];
}
