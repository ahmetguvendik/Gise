using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Gise.HealthCheck.Api.Checks;

// Loglardaki hata sayısı (Telgraf'taki ErrorRateHealthCheck): son N dakikada Error seviyesinde kaç log var, eşik aşılırsa
// sağlıksız. Bir servis çalışıyor ama sürekli hata veriyorsa /health yeşil kalır; bu kontrol onu yakalar. Yalnızca API ve
// worker logları sayılır (denetim akışı insanların işlemleri). Sorgu yapılamıyorsa "hata yok" denmez: uyarının kör kaldığı
// da bilinmeli.
internal sealed class ErrorRateHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _countUrl;
    private readonly int _windowMinutes;
    private readonly int _threshold;
    private readonly AuthenticationHeaderValue? _authorization;

    public ErrorRateHealthCheck(IHttpClientFactory httpClientFactory, string baseUrl, string indexPattern, int windowMinutes, int threshold, string? username, string? password)
    {
        _httpClientFactory = httpClientFactory;
        _countUrl = $"{baseUrl.TrimEnd('/')}/{indexPattern}/_count";
        _windowMinutes = windowMinutes;
        _threshold = threshold;
        if (!string.IsNullOrEmpty(username))
            _authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}")));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new
            {
                query = new
                {
                    @bool = new
                    {
                        filter = new object[]
                        {
                            new { term = new Dictionary<string, string> { ["log.level"] = "Error" } },
                            new { range = new Dictionary<string, object> { ["@timestamp"] = new { gte = $"now-{_windowMinutes}m" } } }
                        }
                    }
                }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, _countUrl) { Content = JsonContent.Create(query), Headers = { Authorization = _authorization } };
            using var response = await _httpClientFactory.CreateClient(Probe.HttpClientName).SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
                return new HealthCheckResult(context.Registration.FailureStatus, $"Hata sayısı okunamadı: Elasticsearch HTTP {(int)response.StatusCode} döndü.");

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var count = document.RootElement.GetProperty("count").GetInt32();

            return count > _threshold
                ? new HealthCheckResult(context.Registration.FailureStatus, $"Son {_windowMinutes} dakikada {count} hata var (eşik {_threshold}). Loglar: Kibana → Gişe API logları, Gişe worker logları.")
                : HealthCheckResult.Healthy($"Son {_windowMinutes} dakikada {count} hata (eşik {_threshold}).");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException && !cancellationToken.IsCancellationRequested)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, $"Hata oranı sorgulanamadı: {Probe.Reason(ex)}.");
        }
    }
}
