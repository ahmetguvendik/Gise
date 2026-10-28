using Gise.Reservation.Application.Interfaces;
using Gise.Reservation.Application.UnitOfWorks;
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
        services.AddDbContext<ReservationDbContext>(options => options
            .UseNpgsql(configuration.GetConnectionString("Reservation"))
            .UseSnakeCaseNamingConvention());

        services.AddScoped<IEventStockRepository, EventStockRepository>();
        services.AddScoped<ISeatHoldRepository, SeatHoldRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
