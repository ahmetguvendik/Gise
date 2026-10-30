using Gise.Contracts.Grpc.Catalog;
using Gise.HealthChecks;
using Gise.Search.Infrastructure;
using Gise.Search.Infrastructure.Elasticsearch;
using Gise.Search.SyncWorker;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Catalog'un gRPC ucu: yalnızca Docker ağının içinden, 8081 (HTTP/2, TLS'siz).
var catalogGrpcUrl = builder.Configuration.GetRequiredSection("Catalog")["GrpcUrl"]
    ?? throw new InvalidOperationException("Catalog:GrpcUrl ayarı eksik.");
builder.Services.AddGrpcClient<CatalogGrpc.CatalogGrpcClient>(options => options.Address = new Uri(catalogGrpcUrl));

// Arama dizini (Elasticsearch).
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddHostedService<Worker>();

// Eşik yenileme aralığının 2 katı: tek bir yavaş tur yanlış alarm üretmesin.
var intervalSeconds = builder.Configuration.GetRequiredSection("Sync").GetValue<int>("IntervalSeconds");

builder.Services.AddHealthChecks()
    .AddWorkerHeartbeat(TimeSpan.FromSeconds(intervalSeconds * 2))
    .AddCheck<ElasticsearchHealthCheck>("elasticsearch", tags: ["ready"]);

var app = builder.Build();

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

app.Run();
