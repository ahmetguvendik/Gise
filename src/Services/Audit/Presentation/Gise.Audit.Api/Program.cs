using System.Diagnostics;
using Gise.Audit.Api.Authentication;
using Gise.Audit.Api.ErrorHandling;
using Gise.Audit.Application;
using Gise.Audit.Infrastructure;
using Gise.Audit.Infrastructure.Elasticsearch;
using Gise.Authentication;
using Gise.HealthChecks;
using Gise.Logging;

var builder = WebApplication.CreateBuilder(args);

// Loglar Kafka üzerinden Elasticsearch'e, izler Elastic APM'e (bkz. tasarım 8).
builder.UseAppLogging("audit-api");
builder.UseAppApm("audit-api");

// Model bağlama aşamasındaki 400'ler de Türkçe ve aynı biçimde (ErrorHandling/ModelValidationResponses).
builder.Services.AddControllers().AddTurkishModelValidation();

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

builder.Services.AddKeycloakAuthentication(builder.Configuration).AddAuditPolicies();
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Veritabanı yok; kayıtlar Elasticsearch'te. Kapalıyken yalnızca denetim sayfası çalışmaz: Degraded (ready 200).
builder.Services.AddHealthChecks()
    .AddCheck<ElasticsearchHealthCheck>("elasticsearch", tags: ["ready"]);

var app = builder.Build();

// Her istek için tek satır (bkz. tasarım 8); en başta: hata yanıtları da yazılır.
app.UseAppRequestLogging();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapAppHealthChecks();

app.Run();
