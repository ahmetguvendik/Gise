using Gise.Catalog.Application.Interfaces;
using Gise.Catalog.Domain.Entities;
using Gise.Catalog.Domain.Enums;
using Gise.Catalog.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Gise.Catalog.Persistence.Repositories;

public sealed class EventRepository : IEventRepository
{
    private readonly CatalogDbContext _context;

    public EventRepository(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Event entity, CancellationToken cancellationToken)
        => await _context.Events.AddAsync(entity, cancellationToken);

    public Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _context.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Event>> GetUpcomingPublishedAsync(DateTime from, CancellationToken cancellationToken)
        => await _context.Events
            .AsNoTracking()
            .Where(e => e.Status == EventStatus.Published && e.StartsAt > from)
            .OrderBy(e => e.StartsAt)
            .ToListAsync(cancellationToken);
}
