using Gise.Reservation.Application.Features.Queries.EventStock.Response;
using MediatR;

namespace Gise.Reservation.Application.Features.Queries.EventStock.Request;

// Etkinliklerin koltuk ve satılmamış koltuk sayısı (gRPC: Search'ün "Tükendi" bilgisi için).
public sealed class GetEventAvailabilityQuery : IRequest<IReadOnlyList<GetEventAvailabilityResponse>>
{
    public IReadOnlyCollection<Guid> EventIds { get; set; } = [];
}
