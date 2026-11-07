using FluentValidation;
using Gise.Order.Application.Auditing;
using Gise.Order.Application.Behaviors;
using Gise.Order.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Gise.Order.Application;

public static class ServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ServiceRegistration).Assembly));

        // Denetim kaydı en dışta: doğrulamaya takılan deneme de yazılır.
        services.AddScoped<IChangeAuditCollector, ChangeAuditCollector>();
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CommandLoggingBehavior<,>));

        services.AddValidatorsFromAssembly(typeof(ServiceRegistration).Assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
