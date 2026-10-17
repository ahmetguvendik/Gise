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

    public async Task<IReadOnlyList<Venue>> GetAllAsync(CancellationToken cancellationToken)
        => await _context.Venues
            .AsNoTracking()
            .OrderBy(v => v.City).ThenBy(v => v.Name)
            .ToListAsync(cancellationToken);

    public Task<Venue?> GetWithLayoutAsync(Guid id, CancellationToken cancellationToken)
        => _context.Venues
            .AsNoTracking()
            .Include(v => v.Sections).ThenInclude(s => s.Seats)
            // Tek sorguda bölümler × koltuklar kartezyen çarpımı oluşmasın.
            .AsSplitQuery()
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
}
