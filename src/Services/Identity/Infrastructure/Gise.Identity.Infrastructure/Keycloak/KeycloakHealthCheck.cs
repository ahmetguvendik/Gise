using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Gise.Identity.Infrastructure.Keycloak;

// Olay okuyan servis hesabıyla (gise-identity) token alınabiliyor mu: Keycloak açık ve gizli anahtar doğru. Token
// önbellekte olduğundan çoğu kontrol Keycloak'a gitmez.
public sealed class KeycloakHealthCheck : IHealthCheck
{
    private readonly KeycloakServiceToken _token;

    public KeycloakHealthCheck([FromKeyedServices(ServiceRegistration.EventsAccount)] KeycloakServiceToken token)
    {
        _token = token;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await _token.GetAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Keycloak servis hesabıyla token alınamadı.", ex);
        }
    }
}
