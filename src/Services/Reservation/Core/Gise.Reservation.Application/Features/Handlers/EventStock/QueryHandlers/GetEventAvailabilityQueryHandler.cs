using Gise.Reservation.Application.Features.Queries.EventStock.Request;
using Gise.Reservation.Application.Features.Queries.EventStock.Response;
using Gise.Reservation.Application.Interfaces;
using MediatR;

namespace Gise.Reservation.Application.Features.Handlers.EventStock.QueryHandlers;

public sealed class GetEventAvailabilityQueryHandler : IRequestHandler<GetEventAvailabilityQuery, IReadOnlyList<GetEventAvailabilityResponse>>
{
    private readonly IEventStockRepository _eventStockRepository;

    public GetEventAvailabilityQueryHandler(IEventStockRepository eventStockRepository)
    {
        _eventStockRepository = eventStockRepository;
    }

    public async Task<IReadOnlyList<GetEventAvailabilityResponse>> Handle(GetEventAvailabilityQuery request, CancellationToken cancellationToken)
        => (await _eventStockRepository.GetAvailabilityAsync(request.EventIds, cancellationToken))
            .Select(a => new GetEventAvailabilityResponse { EventId = a.EventId, SeatCount = a.SeatCount, AvailableCount = a.AvailableCount })
            .ToList();
}
