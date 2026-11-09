namespace Gise.Identity.Application.Users;

// Keycloak'taki bir kullanıcının yönetim ekranında gereken bilgileri.
public sealed record KeycloakUser(
    string Id,
    string Email,
    string? FirstName,
    string? LastName,
    bool Enabled,
    bool EmailVerified);
