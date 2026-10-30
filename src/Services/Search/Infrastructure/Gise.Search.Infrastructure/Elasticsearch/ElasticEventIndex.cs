using System.Text.Json;
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

// Her yenilemede yeni bir dizin kurulur ("events-<zaman>"); okuyanlar "events" takma adını kullanır, takma ad yenileme
// bitince yeni dizine geçer. Eşleme değişikliği de böylece bir sonraki yenilemede kendiliğinden gelir. Eşleme ve Türkçe
// çözümleyiciler events-index.json'da.
public sealed class ElasticEventIndex : IEventIndex
{
    private const string Alias = "events";
    private const string IndexPrefix = "events-";
    private const int BulkSize = 500;

    // Başlık en güçlü eşleşme, sonra mekân, sonra şehir.
    private static readonly string[] TextFields = ["title^3", "venueName^2", "city"];
    private static readonly string[] PrefixFields = ["title.suggest^3", "venueName.suggest^2", "city.suggest"];

    private static readonly SortOptions ByDate = new() { Field = new FieldSort { Field = "startsAt", Order = SortOrder.Asc } };

    private readonly ElasticsearchClient _client;

    public ElasticEventIndex(ElasticsearchClient client)
    {
        _client = client;
    }

    public async Task<int> ReplaceAllAsync(IAsyncEnumerable<EventDocument> documents, CancellationToken cancellationToken)
    {
        // Milisaniyesiyle: art arda iki yenileme aynı adı almasın.
        var indexName = $"{IndexPrefix}{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}";
        await CreateIndexAsync(indexName, cancellationToken);

        try
        {
            var count = 0;
            var batch = new List<EventDocument>(BulkSize);
            await foreach (var document in documents.WithCancellation(cancellationToken))
            {
                batch.Add(document);
                if (batch.Count < BulkSize)
                    continue;

                await BulkIndexAsync(indexName, batch, cancellationToken);
                count += batch.Count;
                batch.Clear();
            }

            await BulkIndexAsync(indexName, batch, cancellationToken);
            count += batch.Count;

            // Takma ad taşınınca kayıtlar hemen aranabilsin; Elasticsearch kendiliğinden saniyede bir yeniler.
            EnsureValid(await _client.Indices.RefreshAsync(indexName, cancellationToken), "yenilenemedi");
            await SwitchAliasAsync(indexName, cancellationToken);
            return count;
        }
        catch
        {
            // Yarım dizin kalmasın; silinemezse bir sonraki başarılı yenileme siler.
            await _client.Indices.DeleteAsync(indexName, CancellationToken.None);
            throw;
        }
    }

    private async Task CreateIndexAsync(string indexName, CancellationToken cancellationToken)
    {
        await using var stream = typeof(ElasticEventIndex).Assembly.GetManifestResourceStream("events-index.json")
            ?? throw new InvalidOperationException("events-index.json bulunamadı.");
        using var reader = new StreamReader(stream);
        var body = await reader.ReadToEndAsync(cancellationToken);

        var response = await _client.Transport.RequestAsync<StringResponse>(
            Elastic.Transport.HttpMethod.PUT, $"/{indexName}", PostData.String(body), cancellationToken);
        if (!response.ApiCallDetails.HasSuccessfulStatusCode)
            throw new InvalidOperationException($"Arama dizini oluşturulamadı: {response.Body}");
    }

    private async Task BulkIndexAsync(string indexName, IReadOnlyCollection<EventDocument> documents, CancellationToken cancellationToken)
    {
        if (documents.Count == 0)
            return;

        var request = new BulkRequest(indexName)
        {
            Operations = [.. documents.Select(d => new BulkIndexOperation<EventDocument>(d) { Id = d.Id.ToString() })]
        };
        var response = await _client.BulkAsync(request, cancellationToken);
        EnsureValid(response, "toplu yazılamadı");
        if (response.Errors)
            throw new InvalidOperationException($"Arama dizinine toplu yazmada hata: {response.ItemsWithErrors.First().Error?.Reason}");
    }

    // Takma ad tek istekte yeni dizine geçer: arama hiçbir an boş ya da iki dizinden birden okumaz. Sonra eski dizinler
    // (yarıda kalmış yenilemelerinki dahil) silinir.
    private async Task SwitchAliasAsync(string indexName, CancellationToken cancellationToken)
    {
        // Her dizin ve üzerindeki takma adlar: {"events-...": {"aliases": {"events": {}}}, ...}
        var current = await _client.Transport.RequestAsync<StringResponse>(
            Elastic.Transport.HttpMethod.GET, $"/{IndexPrefix}*/_alias", null, cancellationToken);
        if (!current.ApiCallDetails.HasSuccessfulStatusCode)
            throw new InvalidOperationException($"Arama dizinleri okunamadı: {current.Body}");

        using var indices = JsonDocument.Parse(current.Body);
        var oldIndices = indices.RootElement.EnumerateObject().Where(index => index.Name != indexName).ToList();

        var actions = new List<object> { new { add = new { index = indexName, alias = Alias } } };
        actions.AddRange(oldIndices
            .Where(index => index.Value.GetProperty("aliases").TryGetProperty(Alias, out _))
            .Select(index => new { remove = new { index = index.Name, alias = Alias } }));

        var switched = await _client.Transport.RequestAsync<StringResponse>(
            Elastic.Transport.HttpMethod.POST, "/_aliases", PostData.String(JsonSerializer.Serialize(new { actions })), cancellationToken);
        if (!switched.ApiCallDetails.HasSuccessfulStatusCode)
            throw new InvalidOperationException($"Arama yeni dizine geçirilemedi: {switched.Body}");

        if (oldIndices.Count > 0)
            EnsureValid(await _client.Indices.DeleteAsync(string.Join(',', oldIndices.Select(index => index.Name)), cancellationToken), "silinemedi");
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
