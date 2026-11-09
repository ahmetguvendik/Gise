namespace Gise.Identity.Application.Features.Queries.Account.Response;

public sealed class GetMyActivityResponse
{
    // UTC.
    public DateTime Time { get; set; }

    // Keycloak'ın olay türü: LOGIN, LOGIN_ERROR, LOGOUT, UPDATE_CREDENTIAL, REMOVE_CREDENTIAL, UPDATE_EMAIL... Ön yüz
    // Türkçe karşılığını gösterir.
    public string Type { get; set; } = string.Empty;

    public bool Failed { get; set; }

    // Başarısız olayda Keycloak'ın hata kodu, ör. "invalid_user_credentials" (yanlış parola).
    public string? Error { get; set; }

    // Parola ve iki aşamalı doğrulama olaylarında: "password", "otp".
    public string? CredentialType { get; set; }

    public string? IpAddress { get; set; }
}
