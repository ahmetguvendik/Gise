using System.Diagnostics;
using Gise.Logging;
using Gise.Search.Api.ErrorHandling;
using Gise.Search.Application;
using Gise.Search.Infrastructure;
using Gise.Search.Infrastructure.Elasticsearch;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Loglar Kafka üzerinden Elasticsearch'e, izler Elastic APM'e (bkz. tasarım 8).
builder.UseAppLogging("search-api");
builder.UseAppApm("search-api");

// Model bağlama aşamasındaki 400'ler de Türkçe ve aynı biçimde (ErrorHandling/ModelValidationResponses).
builder.Services.AddControllers().AddTurkishModelValidation();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
{
    // Gövdesiz 404 yanıtları da Türkçe başlıklı ProblemDetails olarak döner.
    context.ProblemDetails.Title = context.ProblemDetails.Status switch
    {
        StatusCodes.Status404NotFound when context.Exception is null => "İstenen adres bulunamadı.",
        _ => context.ProblemDetails.Title
    };
    context.ProblemDetails.Extensions["traceId"] = Activity.Current?.TraceId.ToString() ?? context.HttpContext.TraceIdentifier;
});

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Veritabanı yok; arama kayıtları Elasticsearch'te.
builder.Services.AddHealthChecks()
    .AddCheck<ElasticsearchHealthCheck>("elasticsearch", tags: ["ready"]);

var app = builder.Build();

// Her istek için tek satır (bkz. tasarım 8); en başta: hata yanıtları da yazılır.
app.UseAppRequestLogging();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapControllers();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

app.Run();
