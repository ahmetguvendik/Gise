using Gise.Payment.Application.Interfaces;
using Gise.Payment.Application.UnitOfWorks;
using Gise.Payment.Persistence.Auditing;
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
        services.AddDbContext<PaymentDbContext>((provider, options) =>
        {
            options
                .UseNpgsql(configuration.GetConnectionString("Payment"))
                .UseSnakeCaseNamingConvention();

            // Denetim kaydının "Değişenler" kısmı. Command işlemeyen process'te toplayıcı kayıtlı değil; değişiklik
            // toplanmaz.
            if (provider.GetService<IChangeAuditCollector>() is { } collector)
                options.AddInterceptors(new ChangeAuditInterceptor(collector));
        });

        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IOrderPaymentRepository, OrderPaymentRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
