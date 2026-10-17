using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Gise.Catalog.Application;

public static class ServiceRegistration
{
    // Handler'lar ve pipeline, Api'deki AddMediator ile kaydedilir (kaynak üretici orada çalışır).
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddValidatorsFromAssembly(typeof(ServiceRegistration).Assembly);

        return services;
    }
}
