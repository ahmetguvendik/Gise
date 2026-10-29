using Gise.Ticket.Application.UnitOfWorks;
using Gise.Ticket.Persistence.Contexts;

namespace Gise.Ticket.Persistence.UnitOfWorks;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly TicketDbContext _context;

    public UnitOfWork(TicketDbContext context)
    {
        _context = context;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _context.SaveChangesAsync(cancellationToken);
}
