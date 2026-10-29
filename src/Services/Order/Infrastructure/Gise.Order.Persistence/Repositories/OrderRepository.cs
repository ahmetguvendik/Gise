using Gise.Order.Application.Interfaces;
using Gise.Order.Domain.Enums;
using Gise.Order.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Gise.Order.Persistence.Repositories;

public sealed class OrderRepository : IOrderRepository
{
    private readonly OrderDbContext _context;

    public OrderRepository(OrderDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Domain.Entities.Order order, CancellationToken cancellationToken)
        => await _context.Orders.AddAsync(order, cancellationToken);

    public Task<Domain.Entities.Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public Task<Domain.Entities.Order?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken)
        => _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Domain.Entities.Order>> GetCompletedByEventForUpdateAsync(Guid eventId, CancellationToken cancellationToken)
        => await _context.Orders
            .Where(o => o.EventId == eventId && o.Status == OrderStatus.Completed)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Domain.Entities.Order>> GetByCustomerAsync(string customerId, CancellationToken cancellationToken)
        => await _context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);
}
