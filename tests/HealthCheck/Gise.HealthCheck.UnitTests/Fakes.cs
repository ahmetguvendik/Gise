using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Gise.HealthCheck.UnitTests;

// Verilen yanıtı döner, gelen isteği (adres, gövde, başlık) saklar; sunucu yokmuş gibi hata da fırlatabilir.
public sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage>? respond = null, Exception? throws = null) : HttpMessageHandler
{
    public List<(HttpMethod Method, Uri? Uri, string? Body, string? Authorization)> Requests { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add((request.Method, request.RequestUri,
            request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken),
            request.Headers.Authorization?.ToString()));

        if (throws is not null)
            throw throws;

        return respond?.Invoke(request) ?? new HttpResponseMessage(System.Net.HttpStatusCode.OK);
    }
}

public sealed class FakeHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
}

public static class Contexts
{
    // Kayıt, hata durumunu belirler: vital bileşen Unhealthy, değilse Degraded.
    public static HealthCheckContext For(HealthStatus failureStatus = HealthStatus.Unhealthy) => new()
    {
        Registration = new HealthCheckRegistration("test", _ => null!, failureStatus, tags: null)
    };

    public static HttpResponseMessage Json(string json, System.Net.HttpStatusCode status = System.Net.HttpStatusCode.OK)
        => new(status) { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") };
}
