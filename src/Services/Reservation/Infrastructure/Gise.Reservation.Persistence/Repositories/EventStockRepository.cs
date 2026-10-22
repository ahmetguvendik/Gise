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

    public Task<EventStock?> GetWithSeatsAsync(Guid eventId, CancellationToken cancellationToken)
        => _context.EventStocks
            .AsNoTracking()
            .Include(s => s.Seats)
            .FirstOrDefaultAsync(s => s.Id == eventId, cancellationToken);
}
