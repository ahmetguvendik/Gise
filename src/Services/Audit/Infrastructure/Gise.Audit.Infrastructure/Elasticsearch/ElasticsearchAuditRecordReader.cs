using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Gise.Audit.Application.Common;
using Gise.Audit.Application.Features.Queries.Record.Request;
using Gise.Audit.Application.Features.Queries.Record.Response;
using Gise.Audit.Application.Interfaces;
using Gise.Exceptions;
using Microsoft.Extensions.Configuration;

namespace Gise.Audit.Infrastructure.Elasticsearch;

public sealed class ElasticsearchAuditRecordReader : IAuditRecordReader
{
    private readonly ElasticsearchClient _client;

    public ElasticsearchAuditRecordReader(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _client = new ElasticsearchClient(httpClientFactory, configuration);
    }

    public async Task<PagedResult<SearchAuditRecordsResponse>> SearchAsync(SearchAuditRecordsQuery query, CancellationToken cancellationToken)
    {
        var filters = new List<object>();
        if (!string.IsNullOrEmpty(query.Action)) filters.Add(Term("event.action", query.Action));
        if (!string.IsNullOrEmpty(query.Outcome)) filters.Add(Term("event.outcome", query.Outcome));
        if (!string.IsNullOrEmpty(query.ActorId)) filters.Add(Term("actor.id", query.ActorId));
        if (query.From is not null || query.To is not null)
        {
            var range = new Dictionary<string, string>();
            if (query.From is { } from) range["gte"] = from.UtcDateTime.ToString("O", CultureInfo.InvariantCulture);
            if (query.To is { } to) range["lte"] = to.UtcDateTime.ToString("O", CultureInfo.InvariantCulture);
            filters.Add(new { range = new Dictionary<string, object> { ["@timestamp"] = range } });
        }

        var body = new
        {
            from = (query.Page - 1) * query.PageSize,
            size = query.PageSize,
            track_total_hits = true,
            sort = new object[] { new Dictionary<string, string> { ["@timestamp"] = "desc" } },
            query = new { @bool = new { filter = filters } }
        };

        try
        {
            using var response = await _client.PostAsync("_search", JsonContent.Create(body), cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw ServiceUnavailableException.Elasticsearch($"Elasticsearch HTTP {(int)response.StatusCode} döndü.");

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var hits = document.RootElement.GetProperty("hits");

            return new PagedResult<SearchAuditRecordsResponse>
            {
                Items = hits.GetProperty("hits").EnumerateArray().Select(ToResponse).ToList(),
                TotalCount = hits.GetProperty("total").GetProperty("value").GetInt32(),
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException && !cancellationToken.IsCancellationRequested)
        {
            throw ServiceUnavailableException.Elasticsearch($"Elasticsearch'e ulaşılamadı: {ex.Message}");
        }
    }

    private static object Term(string field, string value) => new { term = new Dictionary<string, string> { [field] = value } };

    private static SearchAuditRecordsResponse ToResponse(JsonElement hit)
    {
        var source = hit.GetProperty("_source");
        var record = new SearchAuditRecordsResponse
        {
            Id = hit.GetProperty("_id").GetString() ?? string.Empty,
            Timestamp = DateTimeOffset.Parse(Text(source, "@timestamp") ?? string.Empty, CultureInfo.InvariantCulture),
            Action = Text(source, "event.action") ?? string.Empty,
            Outcome = Text(source, "event.outcome") ?? "unknown",
            Service = Text(source, "service.name") ?? string.Empty,
            ActorId = Text(source, "actor.id"),
            ClientIp = Text(source, "client.ip"),
            Path = Text(source, "url.path"),
            Message = Text(source, "message"),
            Changes = Text(source, "labels.Changes")
        };

        if (Find(source, "actor.roles") is { ValueKind: JsonValueKind.Array } roles)
            record.ActorRoles = roles.EnumerateArray().Select(role => role.GetString() ?? string.Empty).ToList();

        if (Find(source, "target") is { ValueKind: JsonValueKind.Object } target)
            Flatten(target, string.Empty, record.Target);

        return record;
    }

    // "event.action" hem iç içe ({"event":{"action":..}}) hem düz anahtar ("event.action") olarak gelebilir.
    private static JsonElement? Find(JsonElement element, string path)
    {
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(path, out var direct))
            return direct;

        var current = element;
        foreach (var part in path.Split('.'))
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(part, out current))
                return null;
        }
        return current;
    }

    private static string? Text(JsonElement element, string path)
        => Find(element, path) is { ValueKind: JsonValueKind.String or JsonValueKind.Number } value ? value.ToString() : null;

    // {"role":{"name":"admin"}} → "role.name": "admin"; diziler virgülle birleşir.
    private static void Flatten(JsonElement element, string prefix, Dictionary<string, string> result)
    {
        foreach (var property in element.EnumerateObject())
        {
            var key = prefix.Length == 0 ? property.Name : $"{prefix}.{property.Name}";
            switch (property.Value.ValueKind)
            {
                case JsonValueKind.Object:
                    Flatten(property.Value, key, result);
                    break;
                case JsonValueKind.Array:
                    result[key] = string.Join(", ", property.Value.EnumerateArray().Select(item => item.ToString()));
                    break;
                case JsonValueKind.Null:
                    break;
                default:
                    result[key] = property.Value.ToString();
                    break;
            }
        }
    }
}
