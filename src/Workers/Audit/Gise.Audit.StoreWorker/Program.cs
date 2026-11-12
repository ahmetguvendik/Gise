using Gise.Audit.StoreWorker;
using Gise.HealthChecks;
using Gise.Logging;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Loglar Kafka üzerinden Elasticsearch'e, izler Elastic APM'e (bkz. tasarım 8).
builder.UseAppLogging("audit-store-worker");
builder.UseAppApm("audit-store-worker");

builder.Services.AddDbContext<AuditDbContext>(options => options
    .UseNpgsql(builder.Configuration.GetConnectionString("Audit"))
    .UseSnakeCaseNamingConvention());

builder.Services.AddHostedService<AuditConsumer>();
builder.Services.AddHostedService<RetentionService>();

var staleSeconds = builder.Configuration.GetRequiredSection("Health").GetValue<int>("StaleAfterSeconds");

builder.Services.AddHealthChecks()
    .AddWorkerHeartbeat(TimeSpan.FromSeconds(staleSeconds))
    .AddDbContextCheck<AuditDbContext>("postgres", tags: ["ready"]);

var app = builder.Build();

// Denetim veritabanının şemasını bu worker kurar (başka servis dokunmaz). Aynı anda açılan kopyalar için EF Core
// veritabanında kilit alır; migration'lar sırayla ve bir kez uygulanır.
await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<AuditDbContext>().Database.MigrateAsync();
}

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

app.Run();
