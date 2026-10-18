using Gise.Catalog.Application.Features.Queries.Event.Response;
using MediatR;

namespace Gise.Catalog.Application.Features.Queries.Event.Request;

// gRPC ucundan gelir (GetEventSeats): Reservation koltuk stoğunu bununla kurar.
public sealed class GetEventSeatsQuery : IRequest<GetEventSeatsResponse>
{
    public Guid EventId { get; set; }
}
