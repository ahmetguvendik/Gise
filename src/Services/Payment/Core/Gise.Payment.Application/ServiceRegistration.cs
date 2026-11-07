using Gise.Payment.Application.Auditing;
using Gise.Payment.Application.Behaviors;
using Gise.Payment.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Gise.Payment.Application;

public static class ServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ServiceRegistration).Assembly));

        // Denetim kaydı: her command bitince tek satır (bkz. Behaviors/CommandLoggingBehavior).
        services.AddScoped<IChangeAuditCollector, ChangeAuditCollector>();
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CommandLoggingBehavior<,>));

        return services;
    }
}
