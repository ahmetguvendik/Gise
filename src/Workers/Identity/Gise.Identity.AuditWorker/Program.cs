using Gise.HealthChecks;
using Gise.Identity.Application;
using Gise.Identity.AuditWorker;
using Gise.Identity.Infrastructure;
using Gise.Identity.Infrastructure.Keycloak;
using Gise.Logging;

var builder = WebApplication.CreateBuilder(args);

// Loglar Kafka üzerinden Elasticsearch'e, izler Elastic APM'e (bkz. tasarım 8). Denetim satırları (kişisi belli olanlar)
// logs.audit'e, gerisi logs.workers'a gider (KafkaLogSink).
builder.UseAppLogging("identity-audit-worker");
builder.UseAppApm("identity-audit-worker");

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddHostedService<Worker>();

// Eşik tur aralığının 2 katı: tek bir yavaş tur yanlış alarm üretmesin.
var intervalSeconds = builder.Configuration.GetRequiredSection("EventSync").GetValue<int>("IntervalSeconds");

builder.Services.AddHealthChecks()
    .AddWorkerHeartbeat(TimeSpan.FromSeconds(intervalSeconds * 2))
    .AddCheck<KeycloakHealthCheck>("keycloak", tags: ["ready"]);

var app = builder.Build();

app.MapAppHealthChecks();

app.Run();
