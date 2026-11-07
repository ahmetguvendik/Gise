using Gise.Reservation.Application.Interfaces;
using Gise.Reservation.Application.UnitOfWorks;
using Gise.Reservation.Persistence.Auditing;
using Gise.Reservation.Persistence.Contexts;
using Gise.Reservation.Persistence.Repositories;
using Gise.Reservation.Persistence.UnitOfWorks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gise.Reservation.Persistence;

public static class ServiceRegistration
{
    public static IServiceCollection AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ReservationDbContext>((provider, options) =>
        {
            options
                .UseNpgsql(configuration.GetConnectionString("Reservation"))
                .UseSnakeCaseNamingConvention();

            // Denetim kaydının "Değişenler" kısmı. Command işlemeyen process'te toplayıcı kayıtlı değil; değişiklik
            // toplanmaz.
            if (provider.GetService<IChangeAuditCollector>() is { } collector)
                options.AddInterceptors(new ChangeAuditInterceptor(collector));
        });

        services.AddScoped<IEventStockRepository, EventStockRepository>();
        services.AddScoped<ISeatHoldRepository, SeatHoldRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
