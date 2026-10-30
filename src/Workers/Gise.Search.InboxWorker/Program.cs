using Gise.Contracts.Grpc.Catalog;
using Gise.HealthChecks;
using Gise.Search.Infrastructure;
using Gise.Search.InboxWorker;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<InboxWorkerDbContext>(options => options
    .UseNpgsql(builder.Configuration.GetConnectionString("Search"))
    .UseSnakeCaseNamingConvention());

// Catalog'un gRPC ucu: yalnızca Docker ağının içinden, 8081 (HTTP/2, TLS'siz).
var catalogGrpcUrl = builder.Configuration.GetRequiredSection("Catalog")["GrpcUrl"]
    ?? throw new InvalidOperationException("Catalog:GrpcUrl ayarı eksik.");
builder.Services.AddGrpcClient<CatalogGrpc.CatalogGrpcClient>(options => options.Address = new Uri(catalogGrpcUrl));

// Arama dizini (Elasticsearch).
builder.Services.AddInfrastructureServices(builder.Configuration);

// Kafka'dan okuyup inbox'a yazar; Worker inbox'ı işler.
builder.Services.AddHostedService<CatalogEventsConsumer>();
builder.Services.AddHostedService<Worker>();

// Eşik tarama aralığının 4 katı (en az 30 sn): tek bir yavaş tur yanlış alarm üretmesin.
var pollSeconds = builder.Configuration.GetRequiredSection("Inbox").GetValue<int>("PollIntervalSeconds");

builder.Services.AddHealthChecks()
    .AddWorkerHeartbeat(TimeSpan.FromSeconds(Math.Max(30, pollSeconds * 4)))
    .AddDbContextCheck<InboxWorkerDbContext>("postgres", tags: ["ready"])
    // Dead-letter'a düşmüş olay varsa "Degraded": worker bozuk değil, işlenemeyen bir olay var.
    .AddDbContextCheck<InboxWorkerDbContext>("dead-letters", HealthStatus.Degraded, ["ready"],
        async (db, cancellationToken) => !await db.InboxMessages.AnyAsync(m => m.IsDeadLettered, cancellationToken));

var app = builder.Build();

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

app.Run();
