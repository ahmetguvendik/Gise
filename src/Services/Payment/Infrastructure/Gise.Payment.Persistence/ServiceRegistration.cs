using Gise.Payment.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gise.Payment.Persistence;

public static class ServiceRegistration
{
    public static IServiceCollection AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<PaymentDbContext>(options => options
            .UseNpgsql(configuration.GetConnectionString("Payment"))
            .UseSnakeCaseNamingConvention());

        return services;
    }
}
