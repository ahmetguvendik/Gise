using System.Diagnostics;
using Gise.Authentication;
using Gise.HealthCheck.Api.Authentication;
using Gise.HealthCheck.Api.Checks;
using Gise.HealthCheck.Api.SystemHealth;
using Gise.HealthCheck.Api.Notifications;
using Gise.HealthChecks;
using Gise.Logging;
using HealthChecks.UI.Client;
using HealthChecks.UI.Core.Notifications;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Loglar Kafka üzerinden Elasticsearch'e, izler Elastic APM'e (bkz. tasarım 8).
builder.UseAppLogging("healthcheck-api");
builder.UseAppApm("healthcheck-api");

builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
{
    // Gövdesiz 401/403/404 yanıtları da Türkçe başlıklı ProblemDetails olarak döner.
    context.ProblemDetails.Title = context.ProblemDetails.Status switch
    {
        StatusCodes.Status401Unauthorized => "Bu işlem için giriş yapmalısınız.",
        StatusCodes.Status403Forbidden => "Bu işlem için yetkiniz yok.",
        StatusCodes.Status404NotFound => "İstenen adres bulunamadı.",
        _ => context.ProblemDetails.Title
    };
    context.ProblemDetails.Extensions["traceId"] = Activity.Current?.TraceId.ToString() ?? context.HttpContext.TraceIdentifier;
});

builder.Services.AddKeycloakAuthentication(builder.Configuration).AddHealthCheckPolicies();

// Altyapı bileşenleri ve hata oranı: bu servisin kendi kontrolleri; panel bunları /health/infrastructure ve
// /health/error-rate üzerinden okur.
builder.Services.AddInfrastructureChecks(builder.Configuration);

builder.Services
    .AddHealthChecksUI(settings =>
    {
        settings.SetEvaluationTimeInSeconds(builder.Configuration.GetValue<int>("HealthChecksUI:EvaluationSeconds"));
        settings.SetApiMaxActiveRequests(1);
    })
    // Bellekte: panel yeniden başlayınca geçmiş sıfırlanır. Anlık durum ve düştü/düzeldi bildirimleri için yeterli; kalıcı
    // geçmiş istenirse Postgres sağlayıcısına geçilir.
    .AddInMemoryStorage();

// Düştü ve düzeldi bildirimi: kütüphanenin webhook bildirimcisi yerine nedeni de yazan Slack bildirimcisi.
builder.Services.AddHttpClient(SlackHealthNotifier.HttpClientName, client => client.Timeout = TimeSpan.FromSeconds(10));
builder.Services.Replace(ServiceDescriptor.Singleton<IHealthCheckFailureNotifier, SlackHealthNotifier>());

var app = builder.Build();

// Adres boşsa uyarı gitmez; bunun sessizce olmaması için açılışta söylenir (geliştirmede beklenen durum).
if (app.Services.GetRequiredService<IHealthCheckFailureNotifier>() is SlackHealthNotifier { IsConfigured: false })
    app.Logger.LogWarning("Alerting:Slack:WebhookUrl (HEALTH_WEBHOOK_URL) boş: servis düşünce Slack'e uyarı gitmeyecek; panel ve admin sayfası çalışır.");

// Her istek için tek satır (bkz. tasarım 8); en başta.
app.UseAppRequestLogging();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseAuthentication();
app.UseAuthorization();

// Panelin kendi arayüzü kök adreste; konteynerin 8080'i yalnızca makinenin 127.0.0.1'ine açık ve gateway'de rotası yok.
app.MapHealthChecksUI(options => options.UIPath = "/");

app.MapAppHealthChecks();

// Panel altyapıyı ve hata oranını ayrı kalemler olarak yoklar. Sağlıksız 503, sağlıklı ve Degraded 200.
foreach (var (path, tag) in new[] { ("/health/infrastructure", InfrastructureChecks.InfrastructureTag), ("/health/error-rate", InfrastructureChecks.ErrorRateTag) })
{
    app.MapHealthChecks(path, new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains(tag),
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
        ResultStatusCodes =
        {
            [HealthStatus.Healthy] = StatusCodes.Status200OK,
            [HealthStatus.Degraded] = StatusCodes.Status200OK,
            [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
        }
    }).AllowAnonymous();
}

app.MapSystemHealth();

app.Run();
