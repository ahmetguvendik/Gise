using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Gise.Audit.Infrastructure.Elasticsearch;

// Denetim akışı okunabiliyor mu: Elasticsearch açık ve audit_reader parolası doğru. Elasticsearch kapalıyken yalnızca bu
// servisin sayfası çalışmaz; servis ayakta kalsın diye Degraded döner (ready 200).
public sealed class ElasticsearchHealthCheck : IHealthCheck
{
    private readonly ElasticsearchClient _client;

    public ElasticsearchHealthCheck(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _client = new ElasticsearchClient(httpClientFactory, configuration);
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _client.PostAsync("_count", JsonContent.Create(new { query = new { match_all = new { } } }), cancellationToken);
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Degraded($"Elasticsearch denetim akışını okutmadı (HTTP {(int)response.StatusCode}).");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded("Elasticsearch'e ulaşılamadı.", ex);
        }
    }
}
