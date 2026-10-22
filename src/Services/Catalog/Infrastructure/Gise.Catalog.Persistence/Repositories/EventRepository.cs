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

    public void Remove(Event entity) => _context.Events.Remove(entity);

    public Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _context.Events
            .AsNoTracking()
            .Include(e => e.PriceCategories)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public Task<Event?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken)
        => _context.Events
            .Include(e => e.PriceCategories)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public Task<bool> AnyForVenueAsync(Guid venueId, CancellationToken cancellationToken)
        => _context.Events.AnyAsync(e => e.VenueId == venueId, cancellationToken);

    public async Task<IReadOnlySet<Guid>> GetVenueIdsWithEventsAsync(CancellationToken cancellationToken)
        => (await _context.Events.Select(e => e.VenueId).Distinct().ToListAsync(cancellationToken)).ToHashSet();

    public async Task<IReadOnlyList<Event>> GetByOrganizerAsync(string organizerId, CancellationToken cancellationToken)
        => await _context.Events
            .AsNoTracking()
            .Include(e => e.PriceCategories)
            .Where(e => e.OrganizerId == organizerId)
            .OrderBy(e => e.StartsAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Event>> GetUpcomingPublishedAsync(DateTime from, CancellationToken cancellationToken)
        => await _context.Events
            .AsNoTracking()
            .Include(e => e.PriceCategories)
            .Where(e => e.Status == EventStatus.Published && e.StartsAt > from)
            .OrderBy(e => e.StartsAt)
            .ToListAsync(cancellationToken);
}
