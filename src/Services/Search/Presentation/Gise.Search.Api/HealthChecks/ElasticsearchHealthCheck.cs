using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Gise.Search.Api.HealthChecks;

// Arama Elasticsearch'e bağlı; ulaşılamıyorsa servis hazır sayılmaz.
public sealed class ElasticsearchHealthCheck : IHealthCheck
{
    private readonly ElasticsearchClient _client;

    public ElasticsearchHealthCheck(ElasticsearchClient client)
    {
        _client = client;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var response = await _client.PingAsync(cancellationToken);
        return response.IsValidResponse
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Elasticsearch'e ulaşılamıyor.");
    }
}
