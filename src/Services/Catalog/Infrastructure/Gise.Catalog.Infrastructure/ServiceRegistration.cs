using Gise.Catalog.Application.Interfaces;
using Gise.Catalog.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gise.Catalog.Infrastructure;

public static class ServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // S3 client'ları pahalı ve thread-safe; istek başına yeniden kurulmaz.
        services.AddSingleton<IStorageService, S3StorageService>();

        return services;
    }
}
