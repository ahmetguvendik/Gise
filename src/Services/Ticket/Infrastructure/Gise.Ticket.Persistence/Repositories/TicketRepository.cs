using Gise.Ticket.Application.Interfaces;
using Gise.Ticket.Domain.Enums;
using Gise.Ticket.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Gise.Ticket.Persistence.Repositories;

public sealed class TicketRepository : ITicketRepository
{
    private readonly TicketDbContext _context;

    public TicketRepository(TicketDbContext context)
    {
        _context = context;
    }

    public Task AddRangeAsync(IEnumerable<Domain.Entities.Ticket> tickets, CancellationToken cancellationToken)
        => _context.Tickets.AddRangeAsync(tickets, cancellationToken);

    public Task<bool> AnyForOrderAsync(Guid orderId, CancellationToken cancellationToken)
        => _context.Tickets.AnyAsync(t => t.OrderId == orderId, cancellationToken);

    public Task<bool> AnyActiveForSeatsAsync(Guid eventId, IReadOnlyCollection<Guid> seatIds, CancellationToken cancellationToken)
        => _context.Tickets.AnyAsync(
            t => t.EventId == eventId && seatIds.Contains(t.SeatId) && t.Status != TicketStatus.Cancelled,
            cancellationToken);

    public Task<Domain.Entities.Ticket?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _context.Tickets.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<Domain.Entities.Ticket?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken)
        => _context.Tickets.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<Domain.Entities.Ticket?> GetByCodeAsync(string code, CancellationToken cancellationToken)
        => _context.Tickets.AsNoTracking().FirstOrDefaultAsync(t => t.Code == code, cancellationToken);

    public async Task<IReadOnlyList<Domain.Entities.Ticket>> GetByCustomerAsync(string customerId, Guid? orderId, CancellationToken cancellationToken)
        => await _context.Tickets
            .AsNoTracking()
            .Where(t => t.CustomerId == customerId && (orderId == null || t.OrderId == orderId))
            .OrderBy(t => t.EventStartsAt)
            .ThenBy(t => t.SectionName)
            .ThenBy(t => t.Row.Length)
            .ThenBy(t => t.Row)
            .ThenBy(t => t.Number)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<GateEvent>> GetGateEventsAsync(DateTime startsFrom, DateTime startsTo, CancellationToken cancellationToken)
        => await _context.Tickets
            .AsNoTracking()
            .Where(t => t.Status != TicketStatus.Cancelled && t.EventStartsAt >= startsFrom && t.EventStartsAt <= startsTo)
            .GroupBy(t => new { t.EventId, t.EventTitle, t.EventStartsAt, t.VenueName, t.City })
            .OrderBy(g => g.Key.EventStartsAt)
            .Select(g => new GateEvent(
                g.Key.EventId, g.Key.EventTitle, g.Key.EventStartsAt, g.Key.VenueName, g.Key.City,
                g.Count(), g.Count(t => t.Status == TicketStatus.Used)))
            .ToListAsync(cancellationToken);

    public async Task<bool> TryCheckInAsync(Guid id, string staffId, DateTime checkedInAt, CancellationToken cancellationToken)
        => await _context.Tickets
            .Where(t => t.Id == id && t.Status == TicketStatus.Valid)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(t => t.Status, TicketStatus.Used)
                .SetProperty(t => t.CheckedInAt, checkedInAt)
                .SetProperty(t => t.CheckedInBy, staffId)
                .SetProperty(t => t.UpdatedAt, checkedInAt), cancellationToken) == 1;

    public Task CancelAsync(Guid id, CancellationToken cancellationToken)
        => _context.Tickets
            .Where(t => t.Id == id && t.Status == TicketStatus.Valid)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(t => t.Status, TicketStatus.Cancelled)
                .SetProperty(t => t.UpdatedAt, DateTime.UtcNow), cancellationToken);
}
