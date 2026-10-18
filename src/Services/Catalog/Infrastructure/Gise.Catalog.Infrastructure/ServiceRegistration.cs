using Gise.Catalog.Infrastructure.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Gise.Catalog.Infrastructure;

public static class ServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddHostedService<OutboxProcessor>();

        return services;
    }
}
