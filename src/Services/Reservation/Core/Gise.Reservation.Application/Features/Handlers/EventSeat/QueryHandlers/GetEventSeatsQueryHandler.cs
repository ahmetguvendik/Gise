using Gise.Contracts.Realtime;
using Gise.Exceptions;
using Gise.Reservation.Application.Features.Queries.EventSeat.Request;
using Gise.Reservation.Application.Features.Queries.EventSeat.Response;
using Gise.Reservation.Application.Interfaces;
using Gise.Reservation.Domain.Enums;
using MediatR;

namespace Gise.Reservation.Application.Features.Handlers.EventSeat.QueryHandlers;

public sealed class GetEventSeatsQueryHandler : IRequestHandler<GetEventSeatsQuery, GetEventSeatsResponse>
{
    private readonly IEventStockRepository _eventStockRepository;
    private readonly ISeatLockService _seatLockService;
    private readonly TimeProvider _timeProvider;

    public GetEventSeatsQueryHandler(IEventStockRepository eventStockRepository, ISeatLockService seatLockService, TimeProvider timeProvider)
    {
        _eventStockRepository = eventStockRepository;
        _seatLockService = seatLockService;
        _timeProvider = timeProvider;
    }

    public async Task<GetEventSeatsResponse> Handle(GetEventSeatsQuery request, CancellationToken cancellationToken)
    {
        // Stok yoksa etkinlik ya yayında değil ya da worker koltukları henüz kurmadı.
        var stock = await _eventStockRepository.GetWithSeatsAsync(request.EventId, cancellationToken)
            ?? throw NotFoundException.EventStock(request.EventId);

        // Boş koltuk: satılmamış ve şu an kimse tarafından tutulmuyor.
        var locked = await _seatLockService.GetLockedSeatIdsAsync(stock.Id, stock.Seats.Select(s => s.SeatId).ToList());
        bool IsAvailable(Domain.Entities.EventSeat seat) => seat.Status == SeatStatus.Available && !locked.Contains(seat.SeatId);
        SeatState StateOf(Domain.Entities.EventSeat seat)
            => seat.Status == SeatStatus.Sold ? SeatState.Sold : locked.Contains(seat.SeatId) ? SeatState.Held : SeatState.Available;

        return new GetEventSeatsResponse
        {
            EventId = stock.Id,
            StartsAt = stock.StartsAt,
            IsOnSale = stock.CancelledAt is null && stock.StartsAt > _timeProvider.GetUtcNow().UtcDateTime,
            SoldOut = stock.Seats.Count > 0 && stock.Seats.All(s => s.Status == SeatStatus.Sold),
            Sections = stock.Seats
                .GroupBy(s => new { s.SectionId, s.SectionName })
                .OrderBy(g => g.Key.SectionName)
                .Select(g => new SeatSectionResponse
                {
                    SectionId = g.Key.SectionId,
                    SectionName = g.Key.SectionName,
                    // Fiyat bölüm başına; bölümün bütün koltukları aynı fiyatta.
                    Price = g.First().Price,
                    AvailableCount = g.Count(IsAvailable),
                    // "2", "10"dan önce gelsin: sıra adı önce uzunluğa, sonra metne göre sıralanır.
                    Seats = g
                        .OrderBy(s => s.Row.Length)
                        .ThenBy(s => s.Row)
                        .ThenBy(s => s.Number)
                        .Select(s => new SeatResponse
                        {
                            SeatId = s.SeatId,
                            Row = s.Row,
                            Number = s.Number,
                            IsAvailable = IsAvailable(s),
                            State = StateOf(s).ToString()
                        })
                        .ToList()
                })
                .ToList()
        };
    }
}
