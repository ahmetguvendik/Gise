using Gise.Reservation.Application.Interfaces;
using Gise.Reservation.Domain.Entities;
using Gise.Reservation.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Gise.Reservation.Persistence.Repositories;

public sealed class EventStockRepository : IEventStockRepository
{
    private readonly ReservationDbContext _context;

    public EventStockRepository(ReservationDbContext context)
    {
        _context = context;
    }

    public Task<EventStock?> GetByIdAsync(Guid eventId, CancellationToken cancellationToken)
        => _context.EventStocks.AsNoTracking().FirstOrDefaultAsync(s => s.Id == eventId, cancellationToken);

    public async Task<IReadOnlyList<EventSeat>> GetSeatsAsync(Guid eventId, IReadOnlyCollection<Guid> seatIds, CancellationToken cancellationToken)
        => await _context.EventSeats
            .AsNoTracking()
            .Where(s => s.EventId == eventId && seatIds.Contains(s.SeatId))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<EventSeat>> GetSeatsForUpdateAsync(Guid eventId, IReadOnlyCollection<Guid> seatIds, CancellationToken cancellationToken)
        => await _context.EventSeats
            .Where(s => s.EventId == eventId && seatIds.Contains(s.SeatId))
            .ToListAsync(cancellationToken);

    public Task<EventStock?> GetWithSeatsAsync(Guid eventId, CancellationToken cancellationToken)
        => _context.EventStocks
            .AsNoTracking()
            .Include(s => s.Seats)
            .FirstOrDefaultAsync(s => s.Id == eventId, cancellationToken);
}
