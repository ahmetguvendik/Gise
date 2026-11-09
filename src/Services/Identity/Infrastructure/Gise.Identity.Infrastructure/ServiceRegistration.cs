using Gise.Identity.Application.Interfaces;
using Gise.Identity.Infrastructure.Keycloak;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Gise.Identity.Infrastructure;

public static class ServiceRegistration
{
    public const string HttpClientName = "keycloak";

    // Servis hesapları (KeycloakServiceToken örnekleri, anahtarlı kayıt).
    public const string EventsAccount = "events";
    public const string RolesAccount = "roles";

    // Keycloak'ın olay kaydı: identity-api (giriş geçmişi) ve identity-audit-worker (denetim logu) okur, gise-identity
    // hesabıyla (yalnızca view-events). Ayarlar appsettings'teki Keycloak bölümünden; gizli anahtar compose'dan gelir.
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var keycloak = configuration.GetRequiredSection("Keycloak");
        var timeout = TimeSpan.FromSeconds(keycloak.GetValue<int>("RequestTimeoutSeconds"));

        services.TryAddSingleton(TimeProvider.System);
        services.AddHttpClient(HttpClientName, client => client.Timeout = timeout);
        services.AddKeyedSingleton(EventsAccount, (provider, _) => CreateToken(provider, keycloak, "ServiceClientId", "ServiceClientSecret"));
        services.AddSingleton<IKeycloakAdminClient, KeycloakAdminClient>();

        return services;
    }

    // Rol yönetimi: yalnızca identity-api, ayrı gise-identity-roles hesabıyla (kullanıcı arama ve manage-users). Worker
    // bunu çağırmaz; anahtar ona hiç verilmez.
    public static IServiceCollection AddRoleAdministration(this IServiceCollection services, IConfiguration configuration)
    {
        var keycloak = configuration.GetRequiredSection("Keycloak");

        services.AddKeyedSingleton(RolesAccount, (provider, _) => CreateToken(provider, keycloak, "RolesClientId", "RolesClientSecret"));
        services.AddSingleton<IKeycloakUserAdmin, KeycloakUserAdmin>();

        return services;
    }

    private static KeycloakServiceToken CreateToken(IServiceProvider provider, IConfigurationSection keycloak, string clientIdKey, string secretKey)
    {
        string Required(string key) => keycloak[key] is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"Keycloak:{key} ayarı eksik.");

        return new KeycloakServiceToken(
            provider.GetRequiredService<IHttpClientFactory>(),
            provider.GetRequiredService<TimeProvider>(),
            $"{Required("InternalAuthority")}/protocol/openid-connect/token",
            Required(clientIdKey),
            Required(secretKey));
    }
}
