using Gise.Reservation.Application.Features.Commands.SeatHold.Response;
using Gise.Reservation.Application.Features.Queries.SeatHold.Request;
using Gise.Reservation.Application.Features.Queries.SeatHold.Response;
using Gise.Reservation.Application.Interfaces;
using Gise.Reservation.Domain.Enums;
using Gise.Exceptions;
using MediatR;

namespace Gise.Reservation.Application.Features.Handlers.SeatHold.QueryHandlers;

public sealed class GetHoldByIdQueryHandler : IRequestHandler<GetHoldByIdQuery, GetHoldByIdResponse>
{
    private readonly ISeatHoldRepository _seatHoldRepository;
    private readonly IEventStockRepository _eventStockRepository;
    private readonly TimeProvider _timeProvider;

    public GetHoldByIdQueryHandler(ISeatHoldRepository seatHoldRepository, IEventStockRepository eventStockRepository, TimeProvider timeProvider)
    {
        _seatHoldRepository = seatHoldRepository;
        _eventStockRepository = eventStockRepository;
        _timeProvider = timeProvider;
    }

    public async Task<GetHoldByIdResponse> Handle(GetHoldByIdQuery request, CancellationToken cancellationToken)
    {
        var hold = await _seatHoldRepository.GetByIdAsync(request.HoldId, cancellationToken)
            ?? throw NotFoundException.Hold(request.HoldId);

        if (!request.IsAdmin && hold.UserId != request.UserId)
            throw ForbiddenException.NotHoldOwner(hold.Id);

        var seats = await _eventStockRepository.GetSeatsAsync(hold.EventId, hold.SeatIds, cancellationToken);

        // Süresi dolan tutma kayıtta "Active" kalır (kilit Redis'te kendiliğinden kalktı); dışarıya "Expired" görünür.
        var status = hold.Status == HoldStatus.Active && hold.ExpiresAt <= _timeProvider.GetUtcNow().UtcDateTime
            ? "Expired"
            : hold.Status.ToString();

        return new GetHoldByIdResponse
        {
            HoldId = hold.Id,
            EventId = hold.EventId,
            Status = status,
            ExpiresAt = hold.ExpiresAt,
            TotalPrice = seats.Sum(s => s.Price),
            Seats = seats
                .OrderBy(s => s.SectionName)
                .ThenBy(s => s.Row.Length)
                .ThenBy(s => s.Row)
                .ThenBy(s => s.Number)
                .Select(s => new HeldSeatResponse
                {
                    SeatId = s.SeatId,
                    SectionName = s.SectionName,
                    Row = s.Row,
                    Number = s.Number,
                    Price = s.Price
                })
                .ToList()
        };
    }
}
