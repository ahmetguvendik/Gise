using Gise.Catalog.Application.Interfaces;
using Gise.Catalog.Infrastructure.Caching;
using Gise.Catalog.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Gise.Catalog.Infrastructure;

public static class ServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // S3 client'ları pahalı ve thread-safe; istek başına yeniden kurulmaz.
        services.AddSingleton<IStorageService, S3StorageService>();

        // Önbellek (Redis): koltuk kilitlerini tutan Redis'in aynısı; anahtarlar "catalog:" önekli. Bağlantı pahalı ve
        // thread-safe; uygulama boyunca tek. Redis açılışta yoksa uygulama yine açılır, bağlantı arkada kurulur ve önbellek o
        // sürede atlanır. Önbellek beklemez: bağlantı yokken komut kuyruğa alınmadan hemen düşer (FailFast; yoksa her komut
        // 5 sn kuyrukta bekler), cevap gelmezse kısa zaman aşımıyla düşer; istek veritabanından devam eder.
        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var redis = configuration.GetRequiredSection("Redis");
            var options = ConfigurationOptions.Parse(redis["Connection"] ?? throw new InvalidOperationException("Redis:Connection ayarı eksik."));
            options.AbortOnConnectFail = false;
            options.BacklogPolicy = BacklogPolicy.FailFast;
            var timeout = redis.GetValue("CommandTimeoutMilliseconds", 500);
            options.ConnectTimeout = 2000;
            options.SyncTimeout = timeout;
            options.AsyncTimeout = timeout;
            return ConnectionMultiplexer.Connect(options);
        });
        services.AddSingleton<ICacheService, RedisCacheService>();

        return services;
    }
}
