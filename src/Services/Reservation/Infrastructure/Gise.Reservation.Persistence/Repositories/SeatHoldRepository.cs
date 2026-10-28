using Gise.Reservation.Application.Interfaces;
using Gise.Reservation.Domain.Entities;
using Gise.Reservation.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Gise.Reservation.Persistence.Repositories;

public sealed class SeatHoldRepository : ISeatHoldRepository
{
    private readonly ReservationDbContext _context;

    public SeatHoldRepository(ReservationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(SeatHold hold, CancellationToken cancellationToken)
        => await _context.SeatHolds.AddAsync(hold, cancellationToken);

    public Task<SeatHold?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken)
        => _context.SeatHolds.AsNoTracking().FirstOrDefaultAsync(h => h.OrderId == orderId, cancellationToken);

    public Task<SeatHold?> GetByOrderIdForUpdateAsync(Guid orderId, CancellationToken cancellationToken)
        => _context.SeatHolds.FirstOrDefaultAsync(h => h.OrderId == orderId, cancellationToken);
}
