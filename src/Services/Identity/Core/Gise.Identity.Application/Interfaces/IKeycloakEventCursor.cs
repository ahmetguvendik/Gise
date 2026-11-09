using Gise.Identity.Application.Auditing;

namespace Gise.Identity.Application.Interfaces;

// Denetim loguna en son yazılan Keycloak olayının yeri (bkz. KeycloakEventSync).
public interface IKeycloakEventCursor
{
    ValueTask<KeycloakEventPosition?> ReadAsync(CancellationToken cancellationToken = default);

    ValueTask WriteAsync(KeycloakEventPosition position, CancellationToken cancellationToken = default);
}
