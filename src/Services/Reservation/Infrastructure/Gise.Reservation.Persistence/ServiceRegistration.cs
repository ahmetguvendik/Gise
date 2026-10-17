using Gise.Reservation.Persistence.Contexts;
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

        return services;
    }
}
