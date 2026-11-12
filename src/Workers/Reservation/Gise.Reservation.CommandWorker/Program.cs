using Gise.HealthChecks;
using Gise.Contracts.Commands;
using Gise.Logging;
using Gise.Reservation.Application;
using Gise.Reservation.CommandWorker.Messaging;
using Gise.Reservation.Infrastructure;
using Gise.Reservation.Persistence;
using Gise.Reservation.Persistence.Contexts;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

// Loglar Kafka üzerinden Elasticsearch'e, izler Elastic APM'e (bkz. tasarım 8).
builder.UseAppLogging("reservation-command-worker");
builder.UseAppApm("reservation-command-worker");

// Tüketiciler MediatR komutlarını (HoldSeatsCommand, ReleaseHoldCommand, ConfirmHoldCommand) çağırır. Tabloları reservation-api'nin
// migration'ları kurar; worker migration çalıştırmaz.
builder.Services.AddApplicationServices();
builder.Services.AddPersistenceServices(builder.Configuration);
builder.Services.AddInfrastructureServices(builder.Configuration);

// Ayarlar appsettings'teki RabbitMq bölümünden okunur.
var rabbit = builder.Configuration.GetRequiredSection("RabbitMq");
builder.Services.AddMassTransit(x =>
{
    // MassTransit kullanım verisini kendi sunucusuna gönderiyor (UsageTracker); kapalı.
    x.DisableUsageTelemetry();

    // Inbox: aynı komut iki kez gelirse ikincisi işlenmez. Outbox: cevap (SeatsReserved…) tutma kaydıyla aynı transaction'da
    // yazılır, sonra gönderilir.
    x.AddEntityFrameworkOutbox<ReservationDbContext>(outbox => outbox.UsePostgres());

    x.AddConsumer<ReserveSeatsConsumer>().Endpoint(e => e.Name = Queues.ReserveSeats);
    x.AddConsumer<ReleaseSeatsConsumer>().Endpoint(e => e.Name = Queues.ReleaseSeats);
    x.AddConsumer<ConfirmSeatsConsumer>().Endpoint(e => e.Name = Queues.ConfirmSeats);

    x.AddConfigureEndpointsCallback((context, _, endpoint) =>
    {
        // Geçici hatalar (Redis, veritabanı) birkaç kez denenir; olmazsa mesaj _error kuyruğuna düşer.
        endpoint.UseMessageRetry(retry => retry.Intervals(
            TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15)));
        endpoint.UseEntityFrameworkOutbox<ReservationDbContext>(context);
    });

    x.UsingRabbitMq((context, bus) =>
    {
        bus.Host(rabbit["Host"], "/", host =>
        {
            host.Username(rabbit["Username"] ?? throw new InvalidOperationException("RabbitMq:Username ayarı eksik."));
            host.Password(rabbit["Password"] ?? throw new InvalidOperationException("RabbitMq:Password ayarı eksik."));
        });
        bus.ConfigureEndpoints(context);
    });
});

// MassTransit kendi kontrolünü ("ready" etiketiyle) ekler: RabbitMQ'ya bağlı değilse worker hazır sayılmaz.
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ReservationDbContext>("postgres", tags: ["ready"]);

var app = builder.Build();

app.MapAppHealthChecks();

app.Run();
