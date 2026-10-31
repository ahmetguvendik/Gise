using Gise.SeatMap.Application.Interfaces;
using Gise.SeatMap.Infrastructure.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Gise.SeatMap.Infrastructure;

public static class ServiceRegistration
{
    // Redis: Reservation'ın koltuk değişikliği yayınlarını dinlemek için. Ayar appsettings'teki Redis bölümünden; parola
    // compose'dan ortam değişkeniyle gelir.
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
        services.AddSingleton<ISeatUpdateFeed, RedisSeatUpdateFeed>();

        return services;
    }
}
