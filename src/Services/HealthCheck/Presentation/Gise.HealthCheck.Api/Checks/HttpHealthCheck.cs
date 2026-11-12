using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Gise.HealthCheck.Api.Checks;

// Bileşenin kendi sağlık ucuna GET (Keycloak, MinIO, APISIX, istemci, Kibana, Logstash, APM Server, etcd). Beklenen durum
// kodları ayarla verilir: Kibana kimliksiz 401 döner ama ayaktadır.
internal sealed class HttpHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _url;
    private readonly int[] _acceptedStatuses;

    public HttpHealthCheck(IHttpClientFactory httpClientFactory, string url, int[] acceptedStatuses)
    {
        _httpClientFactory = httpClientFactory;
        _url = url;
        _acceptedStatuses = acceptedStatuses;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClientFactory.CreateClient(Probe.HttpClientName).GetAsync(_url, cancellationToken);
            var status = (int)response.StatusCode;

            var ok = _acceptedStatuses.Length == 0 ? response.IsSuccessStatusCode : _acceptedStatuses.Contains(status);
            return ok
                ? HealthCheckResult.Healthy($"GET {Probe.Display(_url)} → {status}.")
                : new HealthCheckResult(context.Registration.FailureStatus, $"GET {Probe.Display(_url)} → {status}.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, $"GET {Probe.Display(_url)} yanıt vermedi: {Probe.Reason(ex)}.");
        }
    }
}
