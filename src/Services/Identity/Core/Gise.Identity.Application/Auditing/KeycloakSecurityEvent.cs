namespace Gise.Identity.Application.Auditing;

// Keycloak'ın kaydettiği bir kullanıcı olayı: giriş, hatalı giriş, parola değişikliği... Time Unix milisaniye.
public sealed class KeycloakSecurityEvent
{
    public string Id { get; init; } = string.Empty;

    public long Time { get; init; }

    public string Type { get; init; } = string.Empty;

    // Var olmayan bir hesapla denenen girişte boş.
    public string? UserId { get; init; }

    public string? SessionId { get; init; }

    public string? IpAddress { get; init; }

    public string? ClientId { get; init; }

    // Başarısız olaylarda Keycloak'ın hata kodu, ör. "invalid_user_credentials".
    public string? Error { get; init; }

    // Parola ve iki aşamalı doğrulama olaylarında: "password", "otp".
    public string? CredentialType { get; init; }
}
