using Gise.Exceptions;
using Gise.Ticket.Application.UnitOfWorks;
using Gise.Ticket.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Gise.Ticket.Persistence.UnitOfWorks;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly TicketDbContext _context;

    public UnitOfWork(TicketDbContext context)
    {
        _context = context;
    }

    // Sürümü (xmin) izlenen bir satır bu arada değiştiyse 409: ör. aktarım kabul edilirken bilet okutuldu.
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw ConflictException.ConcurrentUpdate();
        }
    }
}
