using Gise.Reservation.Application.UnitOfWorks;
using Gise.Reservation.Persistence.Contexts;

namespace Gise.Reservation.Persistence.UnitOfWorks;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ReservationDbContext _context;

    public UnitOfWork(ReservationDbContext context)
    {
        _context = context;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _context.SaveChangesAsync(cancellationToken);
}
