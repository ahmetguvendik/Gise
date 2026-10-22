using Gise.Reservation.Application.Features.Queries.EventSeat.Request;
using Gise.Reservation.Application.Features.Queries.EventSeat.Response;
using Gise.Reservation.Application.Interfaces;
using Gise.Reservation.Domain.Enums;
using Gise.Exceptions;
using MediatR;

namespace Gise.Reservation.Application.Features.Handlers.EventSeat.QueryHandlers;

public sealed class GetEventSeatsQueryHandler : IRequestHandler<GetEventSeatsQuery, GetEventSeatsResponse>
{
    private readonly IEventStockRepository _eventStockRepository;
    private readonly TimeProvider _timeProvider;

    public GetEventSeatsQueryHandler(IEventStockRepository eventStockRepository, TimeProvider timeProvider)
    {
        _eventStockRepository = eventStockRepository;
        _timeProvider = timeProvider;
    }

    public async Task<GetEventSeatsResponse> Handle(GetEventSeatsQuery request, CancellationToken cancellationToken)
    {
        // Stok yoksa etkinlik ya yayında değil ya da worker koltukları henüz kurmadı.
        var stock = await _eventStockRepository.GetWithSeatsAsync(request.EventId, cancellationToken)
            ?? throw NotFoundException.EventStock(request.EventId);

        return new GetEventSeatsResponse
        {
            EventId = stock.Id,
            StartsAt = stock.StartsAt,
            IsOnSale = stock.CancelledAt is null && stock.StartsAt > _timeProvider.GetUtcNow().UtcDateTime,
            Sections = stock.Seats
                .GroupBy(s => new { s.SectionId, s.SectionName })
                .OrderBy(g => g.Key.SectionName)
                .Select(g => new SeatSectionResponse
                {
                    SectionId = g.Key.SectionId,
                    SectionName = g.Key.SectionName,
                    // Fiyat bölüm başına; bölümün bütün koltukları aynı fiyatta.
                    Price = g.First().Price,
                    AvailableCount = g.Count(s => s.Status == SeatStatus.Available),
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
                            IsAvailable = s.Status == SeatStatus.Available
                        })
                        .ToList()
                })
                .ToList()
        };
    }
}
