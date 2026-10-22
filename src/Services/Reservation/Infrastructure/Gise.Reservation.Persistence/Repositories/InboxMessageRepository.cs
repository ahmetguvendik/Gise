using Gise.Reservation.Application.Interfaces;
using Gise.Reservation.Domain.Entities;
using Gise.Reservation.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Gise.Reservation.Persistence.Repositories;

public sealed class InboxMessageRepository : IInboxMessageRepository
{
    private readonly ReservationDbContext _context;

    public InboxMessageRepository(ReservationDbContext context)
    {
        _context = context;
    }

    public Task<bool> ExistsAsync(string messageId, CancellationToken cancellationToken)
        => _context.InboxMessages.AnyAsync(m => m.MessageId == messageId, cancellationToken);

    public async Task AddAsync(InboxMessage message, CancellationToken cancellationToken)
        => await _context.InboxMessages.AddAsync(message, cancellationToken);
}
