using Gise.Payment.Application.Interfaces;
using Gise.Payment.Application.UnitOfWorks;
using Gise.Payment.Persistence.Contexts;
using Gise.Payment.Persistence.Repositories;
using Gise.Payment.Persistence.UnitOfWorks;
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

        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IOrderPaymentRepository, OrderPaymentRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
