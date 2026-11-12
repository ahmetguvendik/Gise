using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Gise.HealthCheck.Api.Checks;

// Elasticsearch kümesinin durumu (_cluster/health). Tek düğümde yeşil değil sarı normaldir (kopya atanacak ikinci düğüm
// yok); yalnızca kırmızı (birincil parça eksik) sağlıksız sayılır. Salt okunur hesapla (health_reader, "monitor" izni).
internal sealed class ElasticsearchClusterHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _url;
    private readonly AuthenticationHeaderValue? _authorization;

    public ElasticsearchClusterHealthCheck(IHttpClientFactory httpClientFactory, string baseUrl, string? username, string? password)
    {
        _httpClientFactory = httpClientFactory;
        _url = $"{baseUrl.TrimEnd('/')}/_cluster/health";
        if (!string.IsNullOrEmpty(username))
            _authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}")));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, _url) { Headers = { Authorization = _authorization } };
            using var response = await _httpClientFactory.CreateClient(Probe.HttpClientName).SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
                return new HealthCheckResult(context.Registration.FailureStatus, $"Elasticsearch küme durumu okunamadı: HTTP {(int)response.StatusCode}.");

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var status = document.RootElement.GetProperty("status").GetString();

            return status == "red"
                ? new HealthCheckResult(context.Registration.FailureStatus, "Küme kırmızı: eksik birincil parça var.")
                : HealthCheckResult.Healthy($"Küme durumu {status}.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, $"Elasticsearch yanıt vermedi: {Probe.Reason(ex)}.");
        }
    }
}
