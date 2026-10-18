namespace Gise.Catalog.Application.Interfaces;

public interface IEventRepository
{
    Task AddAsync(Domain.Entities.Event entity, CancellationToken cancellationToken);
    // Fiyatlarıyla birlikte, salt okunur.
    Task<Domain.Entities.Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    // Fiyatlarıyla birlikte, değişiklikler izlenir.
    Task<Domain.Entities.Event?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken);

    // Organizatörün tüm etkinlikleri (taslaklar dahil), fiyatlarıyla, tarihe göre sıralı.
    Task<IReadOnlyList<Domain.Entities.Event>> GetByOrganizerAsync(string organizerId, CancellationToken cancellationToken);

    // Fiyatlarıyla birlikte, tarihe göre sıralı.
    Task<IReadOnlyList<Domain.Entities.Event>> GetUpcomingPublishedAsync(DateTime from, CancellationToken cancellationToken);
}
