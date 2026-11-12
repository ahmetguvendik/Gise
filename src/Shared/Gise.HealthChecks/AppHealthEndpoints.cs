using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Gise.HealthChecks;

// Her API ve worker'ın sağlık uçları (Telgraf'taki MapAppHealthChecks); hepsi anonim, yalnızca Docker ağı içinden erişilir
// (gateway'de rotaları yok). Yanıt, sağlık panelinin (HealthCheck.Api) okuduğu JSON biçiminde: her kontrol adıyla, durumuyla
// ve açıklamasıyla görünür.
public static class AppHealthEndpoints
{
    // "ready" etiketli kontroller servisin istek alıp alamayacağını belirler; etiketsizler (ör. tolere edilen kesintiler)
    // yalnızca /health'te görünür.
    public const string ReadyTag = "ready";

    public static IEndpointRouteBuilder MapAppHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        // live hiçbir kontrol çalıştırmaz: süreç ayakta mı. Bağımlılık eklenirse veritabanı takılınca orkestratör bütün
        // servisleri yeniden başlatırdı.
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();

        // ready: yalnızca vazgeçilmez bağımlılıklar (compose ve orkestratör bunu kullanır). Degraded 200: tolere edilen
        // kesinti servisi hazır-değil yapmaz.
        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(ReadyTag),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        }).AllowAnonymous();

        // Hepsi: sağlık paneli bunu yoklar. Sağlıksız 503, sağlıklı ve Degraded 200.
        endpoints.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            }
        }).AllowAnonymous();

        return endpoints;
    }
}
