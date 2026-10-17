namespace Gise.Catalog.Application.Interfaces;

public interface IEventRepository
{
    Task AddAsync(Domain.Entities.Event entity, CancellationToken cancellationToken);
    Task<Domain.Entities.Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Domain.Entities.Event>> GetUpcomingPublishedAsync(DateTime from, CancellationToken cancellationToken);
}
