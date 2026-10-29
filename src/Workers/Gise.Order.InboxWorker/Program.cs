using Gise.HealthChecks;
using Gise.Order.Application;
using Gise.Order.InboxWorker;
using Gise.Order.Persistence;
using Gise.Order.Persistence.Contexts;
using MassTransit;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// İade MediatR komutuyla yapılır (RefundEventOrdersCommand). Tabloları order-api'nin migration'ları kurar; worker
// migration çalıştırmaz.
builder.Services.AddApplicationServices();
builder.Services.AddPersistenceServices(builder.Configuration);

// order-api gibi: RefundPayment outbox'a yazılır, RabbitMQ'ya order-saga-worker gönderir. Bağlantı, hedef adresleri
// doğru olsun diye.
var rabbit = builder.Configuration.GetRequiredSection("RabbitMq");
builder.Services.AddMassTransit(x =>
{
    x.AddEntityFrameworkOutbox<OrderDbContext>(outbox =>
    {
        outbox.UsePostgres();
        outbox.UseBusOutbox(bus => bus.DisableDeliveryService());
    });

    x.UsingRabbitMq((_, bus) => bus.Host(rabbit["Host"], "/", host =>
    {
        host.Username(rabbit["Username"] ?? throw new InvalidOperationException("RabbitMq:Username ayarı eksik."));
        host.Password(rabbit["Password"] ?? throw new InvalidOperationException("RabbitMq:Password ayarı eksik."));
    }));
});

// Kafka'dan okuyup inbox'a yazar; Worker inbox'ı işler.
builder.Services.AddHostedService<CatalogEventsConsumer>();
builder.Services.AddHostedService<Worker>();

// Eşik tarama aralığının 4 katı (en az 30 sn): tek bir yavaş tur yanlış alarm üretmesin.
var pollSeconds = builder.Configuration.GetRequiredSection("Inbox").GetValue<int>("PollIntervalSeconds");

builder.Services.AddHealthChecks()
    .AddWorkerHeartbeat(TimeSpan.FromSeconds(Math.Max(30, pollSeconds * 4)))
    .AddDbContextCheck<OrderDbContext>("postgres", tags: ["ready"])
    // Dead-letter'a düşmüş olay varsa "Degraded": worker bozuk değil, işlenemeyen bir olay var.
    .AddDbContextCheck<OrderDbContext>("dead-letters", HealthStatus.Degraded, ["ready"],
        async (db, cancellationToken) => !await db.InboxMessages.AnyAsync(m => m.IsDeadLettered, cancellationToken));

var app = builder.Build();

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

app.Run();
