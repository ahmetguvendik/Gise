namespace Gise.Catalog.Application.Interfaces;

public interface IVenueRepository
{
    Task AddAsync(Domain.Entities.Venue venue, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);

    // Bölümler ve koltuklar olmadan.
    Task<Domain.Entities.Venue?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Domain.Entities.Venue>> GetAllAsync(CancellationToken cancellationToken);

    // Mekân başına bölüm ve koltuk sayısı; listede yerleşimi yüklemeden göstermek için.
    Task<IReadOnlyDictionary<Guid, (int SectionCount, int SeatCount)>> GetLayoutStatsAsync(CancellationToken cancellationToken);

    // Değişiklikler izlenir; bölümler olmadan.
    Task<Domain.Entities.Venue?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken);

    // Değişiklikler izlenir; bölümler ve koltuklarla.
    Task<Domain.Entities.Venue?> GetWithLayoutForUpdateAsync(Guid id, CancellationToken cancellationToken);

    void Remove(Domain.Entities.Venue venue);

    // Koltuklar olmadan, yalnızca bölümler.
    Task<IReadOnlyList<Domain.Entities.Section>> GetSectionsAsync(Guid venueId, CancellationToken cancellationToken);

    // Bölümler ve koltuklarla birlikte.
    Task<Domain.Entities.Venue?> GetWithLayoutAsync(Guid id, CancellationToken cancellationToken);
}
