using Gise.Logging;
using Gise.SeatMap.Api.Hubs;
using Gise.SeatMap.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Loglar Kafka üzerinden Elasticsearch'e, izler Elastic APM'e (bkz. tasarım 8).
builder.UseAppLogging("seatmap-api");
builder.UseAppApm("seatmap-api");

builder.Services.AddSignalR();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddHostedService<SeatUpdatesRelay>();

builder.Services.AddHealthChecks()
    .AddCheck<RedisHealthCheck>("redis", tags: ["ready"]);

var app = builder.Build();

// Her istek için tek satır (bkz. tasarım 8); en başta: hata yanıtları da yazılır.
app.UseAppRequestLogging();

// Tarayıcı gateway üzerinden WebSocket ile bağlanır (/hubs/seatmap).
app.MapHub<SeatMapHub>("/hubs/seatmap");
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

app.Run();
