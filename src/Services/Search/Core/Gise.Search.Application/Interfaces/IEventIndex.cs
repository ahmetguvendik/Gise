using Gise.Search.Domain.Documents;

namespace Gise.Search.Application.Interfaces;

// Elasticsearch'teki etkinlik dizini: Catalog'un okumaya özel kopyası. search-sync-worker düzenli olarak baştan kurar,
// search-api okur.
public interface IEventIndex
{
    // Kayıtlarla yeni bir dizin kurar; arama tek adımda ona geçer, eski dizin silinir. Yarıda kalırsa arama eski dizinle
    // devam eder. Yazılan kayıt sayısını döndürür.
    Task<int> ReplaceAllAsync(IAsyncEnumerable<EventDocument> documents, CancellationToken cancellationToken);

    Task<EventSearchResult> SearchAsync(EventSearchCriteria criteria, CancellationToken cancellationToken);

    Task<EventSuggestions> SuggestAsync(string text, DateTime from, int size, CancellationToken cancellationToken);
}

// Text boşsa yaklaşan etkinlikler tarihe göre; doluysa en ilgili önce. From ve To UTC.
public sealed record EventSearchCriteria(string? Text, string? City, DateTime From, DateTime? To, int Skip, int Take);

// Cities: şehir filtresinden bağımsız sayılır; kullanıcı diğer şehirleri de görür.
public sealed record EventSearchResult(IReadOnlyList<EventDocument> Events, long Total, IReadOnlyList<CityCount> Cities);

public sealed record CityCount(string City, long Count);

public sealed record EventSuggestions(IReadOnlyList<EventDocument> Events, IReadOnlyList<string> Venues, IReadOnlyList<string> Cities);
