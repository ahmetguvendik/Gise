using Gise.Search.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gise.Search.Persistence;

public static class ServiceRegistration
{
    public static IServiceCollection AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<SearchDbContext>(options => options
            .UseNpgsql(configuration.GetConnectionString("Search"))
            .UseSnakeCaseNamingConvention());

        return services;
    }
}
