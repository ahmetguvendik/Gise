using Gise.Catalog.Application.Interfaces;
using Gise.Catalog.Application.UnitOfWorks;
using Gise.Catalog.Persistence.Auditing;
using Gise.Catalog.Persistence.Contexts;
using Gise.Catalog.Persistence.Repositories;
using Gise.Catalog.Persistence.UnitOfWorks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gise.Catalog.Persistence;

public static class ServiceRegistration
{
    public static IServiceCollection AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<CatalogDbContext>((provider, options) =>
        {
            options
                .UseNpgsql(configuration.GetConnectionString("Catalog"))
                .UseSnakeCaseNamingConvention();

            // Denetim kaydının "Değişenler" kısmı. Command işlemeyen process'te toplayıcı kayıtlı değil; değişiklik
            // toplanmaz.
            if (provider.GetService<IChangeAuditCollector>() is { } collector)
                options.AddInterceptors(new ChangeAuditInterceptor(collector));
        });

        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IVenueRepository, VenueRepository>();
        services.AddScoped<IOutboxMessageRepository, OutboxMessageRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
