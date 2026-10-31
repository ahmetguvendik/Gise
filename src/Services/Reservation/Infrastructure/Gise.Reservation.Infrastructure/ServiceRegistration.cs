using Gise.Reservation.Application.Interfaces;
using Gise.Reservation.Infrastructure.Locking;
using Gise.Reservation.Infrastructure.Realtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Gise.Reservation.Infrastructure;

public static class ServiceRegistration
{
    // Koltuk kilitleri (Redis): reservation-api koltuk haritası için okur, reservation-command-worker tutar ve bırakır;
    // değişiklikleri de aynı bağlantıyla koltuk haritasına yayınlar.
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Bağlantı pahalı ve thread-safe; uygulama boyunca tek. Redis açılışta yoksa uygulama yine açılır, bağlantı arkada kurulur.
        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var options = ConfigurationOptions.Parse(configuration.GetRequiredSection("Redis")["Connection"]
                ?? throw new InvalidOperationException("Redis:Connection ayarı eksik."));
            options.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(options);
        });
        services.AddSingleton<ISeatLockService, RedisSeatLockService>();
        // Koltuk haritasına anlık güncelleme (Redis yayını, bkz. tasarım 3.11).
        services.AddSingleton<ISeatUpdatePublisher, RedisSeatUpdatePublisher>();

        return services;
    }
}
