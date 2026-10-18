using Gise.Catalog.OutboxWorker;
using Gise.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<OutboxWorkerDbContext>(options => options
    .UseNpgsql(builder.Configuration.GetConnectionString("Catalog"))
    .UseSnakeCaseNamingConvention());

builder.Services.AddHostedService<Worker>();

// Eşik tarama aralığının 4 katı (en az 30 sn): tek bir yavaş tur yanlış alarm üretmesin.
var pollSeconds = builder.Configuration.GetRequiredSection("Outbox").GetValue<int>("PollIntervalSeconds");

builder.Services.AddHealthChecks()
    .AddWorkerHeartbeat(TimeSpan.FromSeconds(Math.Max(30, pollSeconds * 4)))
    .AddDbContextCheck<OutboxWorkerDbContext>("postgres", tags: ["ready"])
    // Dead-letter'a düşmüş olay varsa "Degraded": worker bozuk değil, gönderilemeyen bir olay var.
    .AddDbContextCheck<OutboxWorkerDbContext>("dead-letters", HealthStatus.Degraded, ["ready"],
        async (db, cancellationToken) => !await db.OutboxMessages.AnyAsync(m => m.IsDeadLettered, cancellationToken));

var app = builder.Build();

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

app.Run();
