using Gise.Catalog.Application.Interfaces;
using Gise.Catalog.Domain.Entities;
using Gise.Catalog.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Gise.Catalog.Persistence.Repositories;

public sealed class VenueRepository : IVenueRepository
{
    private readonly CatalogDbContext _context;

    public VenueRepository(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Venue venue, CancellationToken cancellationToken)
        => await _context.Venues.AddAsync(venue, cancellationToken);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
        => _context.Venues.AnyAsync(v => v.Id == id, cancellationToken);

    public Task<Venue?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _context.Venues.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Venue>> GetAllAsync(CancellationToken cancellationToken)
        => await _context.Venues
            .AsNoTracking()
            .OrderBy(v => v.City).ThenBy(v => v.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, (int SectionCount, int SeatCount)>> GetLayoutStatsAsync(CancellationToken cancellationToken)
    {
        var sections = await _context.Sections
            .GroupBy(s => s.VenueId)
            .Select(g => new { VenueId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.VenueId, x => x.Count, cancellationToken);

        var seats = await _context.Seats
            .Join(_context.Sections, seat => seat.SectionId, section => section.Id, (_, section) => section.VenueId)
            .GroupBy(venueId => venueId)
            .Select(g => new { VenueId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.VenueId, x => x.Count, cancellationToken);

        return sections.ToDictionary(s => s.Key, s => (s.Value, seats.GetValueOrDefault(s.Key)));
    }

    public Task<Venue?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken)
        => _context.Venues.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    public Task<Venue?> GetWithLayoutForUpdateAsync(Guid id, CancellationToken cancellationToken)
        => _context.Venues
            .Include(v => v.Sections).ThenInclude(s => s.Seats)
            .AsSplitQuery()
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    public void Remove(Venue venue) => _context.Venues.Remove(venue);

    public async Task<IReadOnlyList<Section>> GetSectionsAsync(Guid venueId, CancellationToken cancellationToken)
        => await _context.Sections
            .AsNoTracking()
            .Where(s => s.VenueId == venueId)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);

    public Task<Venue?> GetWithLayoutAsync(Guid id, CancellationToken cancellationToken)
        => _context.Venues
            .AsNoTracking()
            .Include(v => v.Sections).ThenInclude(s => s.Seats)
            // Tek sorguda bölümler × koltuklar kartezyen çarpımı oluşmasın.
            .AsSplitQuery()
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
}
