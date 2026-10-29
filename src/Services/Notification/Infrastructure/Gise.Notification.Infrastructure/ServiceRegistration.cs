using Gise.Notification.Application.Interfaces;
using Gise.Notification.Infrastructure.Email;
using Microsoft.Extensions.DependencyInjection;

namespace Gise.Notification.Infrastructure;

public static class ServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddSingleton<IEmailSender, SmtpEmailSender>();

        return services;
    }
}
