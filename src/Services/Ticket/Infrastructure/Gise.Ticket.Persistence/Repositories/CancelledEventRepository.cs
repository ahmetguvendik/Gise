using Gise.Ticket.Application.Interfaces;
using Gise.Ticket.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Gise.Ticket.Persistence.Repositories;

public sealed class CancelledEventRepository : ICancelledEventRepository
{
    private readonly TicketDbContext _context;

    public CancelledEventRepository(TicketDbContext context)
    {
        _context = context;
    }

    public Task<bool> ExistsAsync(Guid eventId, CancellationToken cancellationToken)
        => _context.CancelledEvents.AnyAsync(e => e.Id == eventId, cancellationToken);
}
