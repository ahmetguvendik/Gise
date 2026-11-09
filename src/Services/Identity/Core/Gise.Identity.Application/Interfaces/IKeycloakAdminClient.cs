using Gise.Identity.Application.Auditing;

namespace Gise.Identity.Application.Interfaces;

// Keycloak'ın yönetim API'si, gise-identity servis hesabıyla. Hesabın tek yetkisi olayları okumak (view-events): kullanıcı
// açamaz, rol veremez, parola değiştiremez.
public interface IKeycloakAdminClient
{
    // Yeniden eskiye, first'ten başlayarak en fazla max olay. userId verilirse yalnızca o kullanıcınınkiler.
    Task<IReadOnlyList<KeycloakSecurityEvent>> GetSecurityEventsAsync(
        IReadOnlyCollection<string> types,
        string? userId,
        int first,
        int max,
        CancellationToken cancellationToken = default);
}
