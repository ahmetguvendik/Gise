using Gise.Payment.Application.Interfaces;
using Gise.Payment.Infrastructure.PaymentProvider;
using Microsoft.Extensions.DependencyInjection;

namespace Gise.Payment.Infrastructure;

public static class ServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // 0009 kartının "ilk deneme" bilgisini tuttuğu için uygulama boyunca tek.
        services.AddSingleton<IPaymentProvider, TestPaymentProvider>();

        return services;
    }
}
