using MediatR;

namespace Gise.Catalog.Application.Features.Commands.Venue.Request;

// Yalnızca etkinliği olmayan mekân silinir; bölümler ve koltuklar da gider.
public sealed class DeleteVenueCommand : IRequest
{
    public Guid Id { get; set; }
}
