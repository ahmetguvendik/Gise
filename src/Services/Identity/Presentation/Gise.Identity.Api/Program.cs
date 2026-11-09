using System.Diagnostics;
using Gise.Authentication;
using Gise.Identity.Api.ErrorHandling;
using Gise.Identity.Application;
using Gise.Identity.Infrastructure;
using Gise.Identity.Infrastructure.Keycloak;
using Gise.Logging;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Loglar Kafka üzerinden Elasticsearch'e, izler Elastic APM'e (bkz. tasarım 8).
builder.UseAppLogging("identity-api");
builder.UseAppApm("identity-api");

builder.Services.AddControllers();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
{
    // Gövdesiz 401/403/404 yanıtları da Türkçe başlıklı ProblemDetails olarak döner.
    context.ProblemDetails.Title = context.ProblemDetails.Status switch
    {
        StatusCodes.Status401Unauthorized => "Bu işlem için giriş yapmalısınız.",
        StatusCodes.Status403Forbidden when context.Exception is null => "Bu işlem için yetkiniz yok.",
        StatusCodes.Status404NotFound when context.Exception is null => "İstenen adres bulunamadı.",
        _ => context.ProblemDetails.Title
    };
    context.ProblemDetails.Extensions["traceId"] = Activity.Current?.TraceId.ToString() ?? context.HttpContext.TraceIdentifier;
});

// Uçların hepsi yalnızca giriş ister; role bağlı policy yok.
builder.Services.AddKeycloakAuthentication(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Veritabanı yok; kullanıcılar ve olaylar Keycloak'ta.
builder.Services.AddHealthChecks()
    .AddCheck<KeycloakHealthCheck>("keycloak", tags: ["ready"]);

var app = builder.Build();

// Her istek için tek satır (bkz. tasarım 8); en başta: hata yanıtları da yazılır.
app.UseAppRequestLogging();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

app.Run();
