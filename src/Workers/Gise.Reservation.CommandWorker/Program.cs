using Gise.Contracts.Commands;
using Gise.Reservation.Application;
using Gise.Reservation.CommandWorker.Messaging;
using Gise.Reservation.Infrastructure;
using Gise.Reservation.Persistence;
using Gise.Reservation.Persistence.Contexts;
using MassTransit;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Tüketiciler MediatR komutlarını (HoldSeatsCommand, ReleaseHoldCommand) çağırır. Tabloları reservation-api'nin
// migration'ları kurar; worker migration çalıştırmaz.
builder.Services.AddApplicationServices();
builder.Services.AddPersistenceServices(builder.Configuration);
builder.Services.AddInfrastructureServices(builder.Configuration);

// Ayarlar appsettings'teki RabbitMq bölümünden okunur.
var rabbit = builder.Configuration.GetRequiredSection("RabbitMq");
builder.Services.AddMassTransit(x =>
{
    // Inbox: aynı komut iki kez gelirse ikincisi işlenmez. Outbox: cevap (SeatsReserved…) tutma kaydıyla aynı transaction'da
    // yazılır, sonra gönderilir.
    x.AddEntityFrameworkOutbox<ReservationDbContext>(outbox => outbox.UsePostgres());

    x.AddConsumer<ReserveSeatsConsumer>().Endpoint(e => e.Name = Queues.ReserveSeats);
    x.AddConsumer<ReleaseSeatsConsumer>().Endpoint(e => e.Name = Queues.ReleaseSeats);

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

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

app.Run();
