using Gise.Reservation.Infrastructure.Consumers;
using Microsoft.Extensions.DependencyInjection;

namespace Gise.Reservation.Infrastructure;

public static class ServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddHostedService<CatalogEventsConsumer>();

        return services;
    }
}
