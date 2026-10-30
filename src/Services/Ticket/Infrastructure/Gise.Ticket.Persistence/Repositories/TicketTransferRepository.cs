using Gise.Ticket.Application.Interfaces;
using Gise.Ticket.Domain.Entities;
using Gise.Ticket.Domain.Enums;
using Gise.Ticket.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Gise.Ticket.Persistence.Repositories;

public sealed class TicketTransferRepository : ITicketTransferRepository
{
    private readonly TicketDbContext _context;

    public TicketTransferRepository(TicketDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(TicketTransfer transfer, CancellationToken cancellationToken)
        => await _context.TicketTransfers.AddAsync(transfer, cancellationToken);

    public Task<TicketTransfer?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _context.TicketTransfers.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<TicketTransfer?> GetPendingForTicketAsync(Guid ticketId, CancellationToken cancellationToken)
        => _context.TicketTransfers.FirstOrDefaultAsync(
            t => t.TicketId == ticketId && t.Status == TicketTransferStatus.Pending, cancellationToken);

    public async Task<IReadOnlyList<TicketTransfer>> GetPendingForTicketsAsync(IReadOnlyCollection<Guid> ticketIds, CancellationToken cancellationToken)
        => await _context.TicketTransfers
            .AsNoTracking()
            .Where(t => ticketIds.Contains(t.TicketId) && t.Status == TicketTransferStatus.Pending)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<(TicketTransfer Transfer, Domain.Entities.Ticket Ticket)>> GetAcceptedFromCustomerAsync(
        string customerId, Guid? orderId, CancellationToken cancellationToken)
    {
        var rows = await _context.TicketTransfers
            .AsNoTracking()
            .Where(t => t.FromCustomerId == customerId && t.Status == TicketTransferStatus.Accepted)
            .Join(_context.Tickets.AsNoTracking(), transfer => transfer.TicketId, ticket => ticket.Id,
                (transfer, ticket) => new { Transfer = transfer, Ticket = ticket })
            .Where(x => orderId == null || x.Ticket.OrderId == orderId)
            .ToListAsync(cancellationToken);

        return rows.Select(x => (x.Transfer, x.Ticket)).ToList();
    }

    public async Task<IReadOnlyList<(TicketTransfer Transfer, Domain.Entities.Ticket Ticket)>> GetPendingForEmailAsync(
        string email, CancellationToken cancellationToken)
    {
        var rows = await _context.TicketTransfers
            .AsNoTracking()
            .Where(t => t.ToEmail == email && t.Status == TicketTransferStatus.Pending)
            .Join(_context.Tickets.AsNoTracking(), transfer => transfer.TicketId, ticket => ticket.Id,
                (transfer, ticket) => new { Transfer = transfer, Ticket = ticket })
            .OrderBy(x => x.Ticket.EventStartsAt)
            .ToListAsync(cancellationToken);

        return rows.Select(x => (x.Transfer, x.Ticket)).ToList();
    }
}
