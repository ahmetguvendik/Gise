using Gise.Search.Domain.Documents;

namespace Gise.Search.Application.Interfaces;

// Elasticsearch'teki etkinlik dizini: Catalog'un okumaya özel kopyası. search-inbox-worker yazar, search-api okur.
public interface IEventIndex
{
    // Dizin yoksa eşlemesi ve Türkçe çözümleyicileriyle oluşturur.
    Task EnsureCreatedAsync(CancellationToken cancellationToken);

    Task<long> CountAsync(CancellationToken cancellationToken);

    // Kayıt varsa üzerine yazar; olay ikinci kez işlense de sonuç aynı.
    Task UpsertAsync(EventDocument document, CancellationToken cancellationToken);

    Task UpsertManyAsync(IReadOnlyCollection<EventDocument> documents, CancellationToken cancellationToken);

    // Kayıt yoksa zararsız.
    Task DeleteAsync(Guid eventId, CancellationToken cancellationToken);

    Task<EventSearchResult> SearchAsync(EventSearchCriteria criteria, CancellationToken cancellationToken);

    Task<EventSuggestions> SuggestAsync(string text, DateTime from, int size, CancellationToken cancellationToken);
}

// Text boşsa yaklaşan etkinlikler tarihe göre; doluysa en ilgili önce. From ve To UTC.
public sealed record EventSearchCriteria(string? Text, string? City, DateTime From, DateTime? To, int Skip, int Take);

// Cities: şehir filtresinden bağımsız sayılır; kullanıcı diğer şehirleri de görür.
public sealed record EventSearchResult(IReadOnlyList<EventDocument> Events, long Total, IReadOnlyList<CityCount> Cities);

public sealed record CityCount(string City, long Count);

public sealed record EventSuggestions(IReadOnlyList<EventDocument> Events, IReadOnlyList<string> Venues, IReadOnlyList<string> Cities);
