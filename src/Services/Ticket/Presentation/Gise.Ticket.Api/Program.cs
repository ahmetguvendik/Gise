using System.Diagnostics;
using Gise.Authentication;
using Gise.Logging;
using Gise.Ticket.Api.Authentication;
using Gise.Ticket.Api.ErrorHandling;
using Gise.Ticket.Application;
using Gise.Ticket.Infrastructure;
using Gise.Ticket.Persistence;
using Gise.Ticket.Persistence.Contexts;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Loglar Kafka üzerinden Elasticsearch'e, izler Elastic APM'e (bkz. tasarım 8).
builder.UseAppLogging("ticket-api");
builder.UseAppApm("ticket-api");

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

builder.Services.AddKeycloakAuthentication(builder.Configuration).AddTicketPolicies();
builder.Services.AddApplicationServices();
builder.Services.AddPersistenceServices(builder.Configuration);
builder.Services.AddInfrastructureServices();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<TicketDbContext>("postgres", tags: ["ready"]);

var app = builder.Build();

// Şema açılışta migration'larla güncellenir. Aynı anda açılan kopyalar için EF Core
// veritabanında kilit alır; migration'lar sırayla ve bir kez uygulanır.
await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<TicketDbContext>().Database.MigrateAsync();
}

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
