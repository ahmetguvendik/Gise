using Gise.Reservation.Application.Interfaces;
using Gise.Reservation.Infrastructure.Consumers;
using Gise.Reservation.Infrastructure.Locking;
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

        return services;
    }
}
