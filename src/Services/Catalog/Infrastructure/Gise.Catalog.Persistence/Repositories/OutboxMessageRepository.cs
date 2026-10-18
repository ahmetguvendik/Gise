using Gise.Catalog.Application.Interfaces;
using Gise.Catalog.Domain.Entities;
using Gise.Catalog.Persistence.Contexts;

namespace Gise.Catalog.Persistence.Repositories;

public sealed class OutboxMessageRepository : IOutboxMessageRepository
{
    private readonly CatalogDbContext _context;

    public OutboxMessageRepository(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(OutboxMessage message, CancellationToken cancellationToken)
        => await _context.OutboxMessages.AddAsync(message, cancellationToken);
}
