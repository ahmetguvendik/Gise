using Gise.Order.Application.Interfaces;
using Gise.Order.Application.UnitOfWorks;
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
        services.AddDbContext<OrderDbContext>(options => options
            .UseNpgsql(configuration.GetConnectionString("Order"))
            .UseSnakeCaseNamingConvention());

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
