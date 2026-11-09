using Gise.Identity.Application.Interfaces;
using Gise.Identity.Infrastructure.Keycloak;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Gise.Identity.Infrastructure;

public static class ServiceRegistration
{
    public const string HttpClientName = "keycloak";

    // Keycloak yönetim API'si: identity-api (giriş geçmişi) ve identity-audit-worker (denetim logu) okur. Ayarlar
    // appsettings'teki Keycloak bölümünden; gizli anahtar compose'dan ortam değişkeniyle gelir.
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var timeout = TimeSpan.FromSeconds(configuration.GetRequiredSection("Keycloak").GetValue<int>("RequestTimeoutSeconds"));

        services.TryAddSingleton(TimeProvider.System);
        services.AddHttpClient(HttpClientName, client => client.Timeout = timeout);
        services.AddSingleton<KeycloakServiceToken>();
        services.AddSingleton<IKeycloakAdminClient, KeycloakAdminClient>();

        return services;
    }
}
