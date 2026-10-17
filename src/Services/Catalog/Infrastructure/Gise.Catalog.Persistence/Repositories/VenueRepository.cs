using Gise.Catalog.Application.Interfaces;
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

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
        => _context.Venues.AnyAsync(v => v.Id == id, cancellationToken);
}
