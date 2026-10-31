namespace Gise.Catalog.Application.Interfaces;

public interface IEventRepository
{
    Task AddAsync(Domain.Entities.Event entity, CancellationToken cancellationToken);
    void Remove(Domain.Entities.Event entity);
    // Fiyatlarıyla birlikte, salt okunur.
    Task<Domain.Entities.Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    // Fiyatlarıyla birlikte, değişiklikler izlenir.
    Task<Domain.Entities.Event?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken);

    // Mekânda (taslak dahil) etkinlik var mı: varsa düzeni değişmez, mekân silinemez.
    Task<bool> AnyForVenueAsync(Guid venueId, CancellationToken cancellationToken);
    Task<IReadOnlySet<Guid>> GetVenueIdsWithEventsAsync(CancellationToken cancellationToken);

    // Organizatörün tüm etkinlikleri (taslaklar dahil), fiyatlarıyla, tarihe göre sıralı.
    Task<IReadOnlyList<Domain.Entities.Event>> GetByOrganizerAsync(string organizerId, CancellationToken cancellationToken);

    // Onay bekleyenler (tüm organizatörler), fiyatlarıyla, tarihe göre sıralı.
    Task<IReadOnlyList<Domain.Entities.Event>> GetPendingReviewAsync(CancellationToken cancellationToken);

    // Yayındaki, başlamamış etkinlikler fiyatlarıyla, sayfa sayfa (Search'ün dizini için); tarihe, aynı tarihtekiler ID'ye
    // göre sıralanır, sayfalar kaymaz.
    Task<IReadOnlyList<Domain.Entities.Event>> GetUpcomingPublishedPageAsync(DateTime from, int skip, int take, CancellationToken cancellationToken);
}
