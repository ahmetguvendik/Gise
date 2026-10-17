using Gise.Ticket.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gise.Ticket.Persistence;

public static class ServiceRegistration
{
    public static IServiceCollection AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<TicketDbContext>(options => options
            .UseNpgsql(configuration.GetConnectionString("Ticket"))
            .UseSnakeCaseNamingConvention());

        return services;
    }
}
