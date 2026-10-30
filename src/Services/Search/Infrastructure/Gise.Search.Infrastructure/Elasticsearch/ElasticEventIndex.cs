using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Aggregations;
using Elastic.Clients.Elasticsearch.Core.Bulk;
using Elastic.Clients.Elasticsearch.Core.Search;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Transport;
using Elastic.Transport.Products.Elasticsearch;
using Gise.Search.Application.Interfaces;
using Gise.Search.Domain.Documents;

namespace Gise.Search.Infrastructure.Elasticsearch;

// Dizin "events-v1" adıyla kurulur, herkes "events" takma adıyla kullanır: eşleme değişince yeni dizin kurulup takma ad
// ona çevrilir, okuyanlar etkilenmez. Eşleme ve Türkçe çözümleyiciler events-index.json'da.
public sealed class ElasticEventIndex : IEventIndex
{
    private const string Alias = "events";
    private const string IndexName = "events-v1";

    // Başlık en güçlü eşleşme, sonra mekân, sonra şehir.
    private static readonly string[] TextFields = ["title^3", "venueName^2", "city"];
    private static readonly string[] PrefixFields = ["title.suggest^3", "venueName.suggest^2", "city.suggest"];

    private static readonly SortOptions ByDate = new() { Field = new FieldSort { Field = "startsAt", Order = SortOrder.Asc } };

    private readonly ElasticsearchClient _client;

    public ElasticEventIndex(ElasticsearchClient client)
    {
        _client = client;
    }

    public async Task EnsureCreatedAsync(CancellationToken cancellationToken)
    {
        var exists = await _client.Indices.ExistsAsync(Alias, cancellationToken);
        if (exists.Exists)
            return;

        await using var stream = typeof(ElasticEventIndex).Assembly.GetManifestResourceStream("events-index.json")
            ?? throw new InvalidOperationException("events-index.json bulunamadı.");
        using var reader = new StreamReader(stream);
        var body = await reader.ReadToEndAsync(cancellationToken);

        var response = await _client.Transport.RequestAsync<StringResponse>(
            Elastic.Transport.HttpMethod.PUT, $"/{IndexName}", PostData.String(body), cancellationToken);

        // Aynı anda açılan başka bir kopya oluşturduysa sorun yok.
        if (!response.ApiCallDetails.HasSuccessfulStatusCode && !response.Body.Contains("resource_already_exists_exception"))
            throw new InvalidOperationException($"Arama dizini oluşturulamadı: {response.Body}");
    }

    public async Task<long> CountAsync(CancellationToken cancellationToken)
    {
        var response = await _client.CountAsync(new CountRequest(Alias), cancellationToken);
        EnsureValid(response, "sayılamadı");
        return response.Count;
    }

    public async Task UpsertAsync(EventDocument document, CancellationToken cancellationToken)
    {
        var response = await _client.IndexAsync(new IndexRequest<EventDocument>(document, Alias, document.Id.ToString()), cancellationToken);
        EnsureValid(response, "yazılamadı");
    }

    public async Task UpsertManyAsync(IReadOnlyCollection<EventDocument> documents, CancellationToken cancellationToken)
    {
        if (documents.Count == 0)
            return;

        var request = new BulkRequest(Alias)
        {
            Operations = [.. documents.Select(d => new BulkIndexOperation<EventDocument>(d) { Id = d.Id.ToString() })]
        };
        var response = await _client.BulkAsync(request, cancellationToken);
        EnsureValid(response, "toplu yazılamadı");
        if (response.Errors)
            throw new InvalidOperationException($"Arama dizinine toplu yazmada hata: {response.ItemsWithErrors.First().Error?.Reason}");
    }

    public async Task DeleteAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var response = await _client.DeleteAsync(new DeleteRequest(Alias, eventId.ToString()), cancellationToken);

        // Kayıt hiç dizinlenmemişse (ör. yayınlanmadan iptal) zararsız.
        if (response.Result == Result.NotFound)
            return;

        EnsureValid(response, "silinemedi");
    }

    public async Task<EventSearchResult> SearchAsync(EventSearchCriteria criteria, CancellationToken cancellationToken)
    {
        var must = new List<Query>();
        if (criteria.Text is not null)
            must.Add(TextQuery(criteria.Text));

        // Şehir filtresi sonuçlara uygulanır, şehir sayımına uygulanmaz: kullanıcı diğer şehirleri de görür.
        Query? cityFilter = null;
        if (criteria.City is not null)
            cityFilter = new TermQuery { Field = "city.keyword", Value = criteria.City };

        var request = new SearchRequest<EventDocument>(Alias)
        {
            Query = new BoolQuery
            {
                Must = must,
                Filter = [DateFilter(criteria.From, criteria.To)]
            },
            PostFilter = cityFilter,
            Aggregations = new Dictionary<string, Aggregation>
            {
                ["cities"] = new TermsAggregation { Field = "city.keyword", Size = 50 }
            },
            // Arama varsa en ilgili önce, yoksa en yakın tarih önce.
            Sort = criteria.Text is null
                ? [ByDate]
                : [new SortOptions { Score = new ScoreSort { Order = SortOrder.Desc } }, ByDate],
            From = criteria.Skip,
            Size = criteria.Take,
            TrackTotalHits = new TrackHits(true)
        };

        var response = await _client.SearchAsync<EventDocument>(request, cancellationToken);
        EnsureValid(response, "aranamadı");

        var cities = response.Aggregations?.GetStringTerms("cities")?.Buckets
            .Select(b => new CityCount(b.Key.ToString(), b.DocCount))
            .ToList() ?? [];

        return new EventSearchResult([.. response.Documents], response.Total, cities);
    }

    public async Task<EventSuggestions> SuggestAsync(string text, DateTime from, int size, CancellationToken cancellationToken)
    {
        var dateFilter = DateFilter(from, null);

        // Üç küçük sorgu aynı anda: başlığı eşleşen etkinlikler, adı eşleşen mekânlar, adı eşleşen şehirler.
        var events = _client.SearchAsync<EventDocument>(new SearchRequest<EventDocument>(Alias)
        {
            Query = PrefixQuery("title.suggest", text, dateFilter),
            Size = size
        }, cancellationToken);
        var venues = _client.SearchAsync<EventDocument>(new SearchRequest<EventDocument>(Alias)
        {
            Query = PrefixQuery("venueName.suggest", text, dateFilter),
            Size = 0,
            Aggregations = new Dictionary<string, Aggregation> { ["names"] = new TermsAggregation { Field = "venueName.keyword", Size = size } }
        }, cancellationToken);
        var cities = _client.SearchAsync<EventDocument>(new SearchRequest<EventDocument>(Alias)
        {
            Query = PrefixQuery("city.suggest", text, dateFilter),
            Size = 0,
            Aggregations = new Dictionary<string, Aggregation> { ["names"] = new TermsAggregation { Field = "city.keyword", Size = size } }
        }, cancellationToken);

        await Task.WhenAll(events, venues, cities);
        EnsureValid(events.Result, "önerilemedi");
        EnsureValid(venues.Result, "önerilemedi");
        EnsureValid(cities.Result, "önerilemedi");

        return new EventSuggestions(
            [.. events.Result.Documents],
            Names(venues.Result),
            Names(cities.Result));
    }

    // İki yol birlikte: kelimenin tamamı (Türkçe kök, küçük yazım hatası toleransı) ya da yazılmakta olan kelimenin başı.
    private static Query TextQuery(string text) => new BoolQuery
    {
        Should =
        [
            new MultiMatchQuery { Query = text, Fields = TextFields, Fuzziness = new Fuzziness("AUTO"), Operator = Operator.And },
            new MultiMatchQuery { Query = text, Fields = PrefixFields, Type = TextQueryType.BoolPrefix, Operator = Operator.And }
        ],
        MinimumShouldMatch = 1
    };

    private static Query PrefixQuery(string field, string text, Query dateFilter) => new BoolQuery
    {
        Must = [new MatchQuery { Field = field, Query = text, Operator = Operator.And }],
        Filter = [dateFilter]
    };

    private static Query DateFilter(DateTime from, DateTime? to)
        => new DateRangeQuery { Field = "startsAt", Gte = from, Lte = to };

    private static IReadOnlyList<string> Names(SearchResponse<EventDocument> response)
        => response.Aggregations?.GetStringTerms("names")?.Buckets.Select(b => b.Key.ToString()).ToList() ?? [];

    private static void EnsureValid(ElasticsearchResponse response, string action)
    {
        if (!response.IsValidResponse)
            throw new InvalidOperationException($"Arama dizini {action}: {response.ElasticsearchServerError?.Error?.Reason ?? response.DebugInformation}");
    }
}
