using System.Diagnostics;
using Gise.Search.Api.ErrorHandling;
using Gise.Search.Api.HealthChecks;
using Gise.Search.Application;
using Gise.Search.Infrastructure;
using Gise.Search.Persistence;
using Gise.Search.Persistence.Contexts;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

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
builder.Services.AddPersistenceServices(builder.Configuration);
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddHealthChecks()
    .AddDbContextCheck<SearchDbContext>("postgres", tags: ["ready"])
    .AddCheck<ElasticsearchHealthCheck>("elasticsearch", tags: ["ready"]);

var app = builder.Build();

// Şema açılışta migration'larla güncellenir (yalnızca inbox tablosu; arama kayıtları Elasticsearch'te).
await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<SearchDbContext>().Database.MigrateAsync();
}

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapControllers();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

app.Run();
