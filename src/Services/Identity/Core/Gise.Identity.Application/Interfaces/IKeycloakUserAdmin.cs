using Gise.Identity.Application.Users;

namespace Gise.Identity.Application.Interfaces;

// Keycloak'ın yönetim API'si, gise-identity-roles servis hesabıyla: kullanıcı arama ve realm rolü verme/alma. Olayları
// okuyan gise-identity hesabından ayrı: audit worker bu yetkiyi almaz. Kullanıcı yoksa NotFoundException.User,
// Keycloak'a ulaşılamazsa ServiceUnavailableException.
public interface IKeycloakUserAdmin
{
    // search boşsa bütün kullanıcılar, doluysa e-posta, ad ya da soyadda geçenler; first'ten başlayarak en fazla max.
    Task<IReadOnlyList<KeycloakUser>> SearchUsersAsync(string search, int first, int max, CancellationToken cancellationToken = default);

    // SearchUsersAsync'in aynı aramayla döndüreceği toplam kullanıcı sayısı.
    Task<int> CountUsersAsync(string search, CancellationToken cancellationToken = default);

    Task<KeycloakUser> GetUserAsync(string userId, CancellationToken cancellationToken = default);

    // Kullanıcıya doğrudan verilmiş realm rolleri (varsayılan rol grubu dahil, içindekiler açılmadan).
    Task<IReadOnlyList<string>> GetRealmRolesAsync(string userId, CancellationToken cancellationToken = default);

    // Zaten varsa bir şey yapmaz.
    Task AddRealmRoleAsync(string userId, string role, CancellationToken cancellationToken = default);

    // Yoksa bir şey yapmaz.
    Task RemoveRealmRoleAsync(string userId, string role, CancellationToken cancellationToken = default);
}
