using FluentValidation;
using Gise.Identity.Application.Auditing;
using Gise.Identity.Application.Behaviors;
using Gise.Identity.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Gise.Identity.Application;

public static class ServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ServiceRegistration).Assembly));

        // Denetim kaydı en dışta: doğrulamaya takılan deneme de yazılır.
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CommandLoggingBehavior<,>));

        services.AddValidatorsFromAssembly(typeof(ServiceRegistration).Assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        // identity-audit-worker: imleç ve hatırlanan oturumlar turlar arasında korunur, ikisi de tek örnek.
        services.AddSingleton<IKeycloakEventCursor, InMemoryKeycloakEventCursor>();
        services.AddSingleton<KeycloakEventSync>();

        return services;
    }
}
