using Gise.Order.Application.Interfaces;
using Gise.Order.Application.UnitOfWorks;
using Gise.Order.Persistence.Auditing;
using Gise.Order.Persistence.Contexts;
using Gise.Order.Persistence.Repositories;
using Gise.Order.Persistence.UnitOfWorks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gise.Order.Persistence;

public static class ServiceRegistration
{
    public static IServiceCollection AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<OrderDbContext>((provider, options) =>
        {
            options
                .UseNpgsql(configuration.GetConnectionString("Order"))
                .UseSnakeCaseNamingConvention();

            // Denetim kaydının "Değişenler" kısmı. Command işlemeyen process'te toplayıcı kayıtlı değil; değişiklik
            // toplanmaz.
            if (provider.GetService<IChangeAuditCollector>() is { } collector)
                options.AddInterceptors(new ChangeAuditInterceptor(collector));
        });

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
