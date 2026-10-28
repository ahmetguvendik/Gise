using Gise.Contracts.Commands;
using Gise.Reservation.Application.Interfaces;
using Gise.Reservation.Infrastructure.Consumers;
using Gise.Reservation.Infrastructure.Locking;
using Gise.Reservation.Infrastructure.Messaging;
using Gise.Reservation.Persistence.Contexts;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Gise.Reservation.Infrastructure;

public static class ServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHostedService<CatalogEventsConsumer>();

        // Bağlantı pahalı ve thread-safe; uygulama boyunca tek. Redis açılışta yoksa uygulama yine açılır, bağlantı arkada kurulur.
        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var options = ConfigurationOptions.Parse(configuration.GetRequiredSection("Redis")["Connection"]
                ?? throw new InvalidOperationException("Redis:Connection ayarı eksik."));
            options.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(options);
        });
        services.AddSingleton<ISeatLockService, RedisSeatLockService>();

        // Order saga'sının komutları (RabbitMQ). Ayarlar appsettings'teki RabbitMq bölümünden okunur.
        var rabbit = configuration.GetRequiredSection("RabbitMq");
        services.AddMassTransit(x =>
        {
            // Inbox: aynı komut iki kez gelirse ikincisi işlenmez. Outbox: cevap (SeatsReserved…) tutma kaydıyla
            // aynı transaction'da yazılır, sonra gönderilir.
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

        return services;
    }
}
