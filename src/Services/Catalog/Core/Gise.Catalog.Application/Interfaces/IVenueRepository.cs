namespace Gise.Catalog.Application.Interfaces;

public interface IVenueRepository
{
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);
}
