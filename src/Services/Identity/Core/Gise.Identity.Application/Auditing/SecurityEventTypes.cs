namespace Gise.Identity.Application.Auditing;

// Keycloak'tan okunan olay türleri. Realm de yalnızca bunları saklar (deploy/keycloak/gise-realm.json, enabledEventTypes);
// liste değişirse ikisi birlikte değişir. Token yenileme (REFRESH_TOKEN), kodun token'a çevrilmesi (CODE_TO_TOKEN) ve
// servis hesabının kendi girişi (CLIENT_LOGIN) saklanmaz. Parola ve iki aşamalı doğrulama değişikliği UPDATE_CREDENTIAL /
// REMOVE_CREDENTIAL ile gelir, CredentialType ayırır; Keycloak'ın aynı anda yazdığı eski adlar (UPDATE_PASSWORD,
// UPDATE_TOTP, REMOVE_TOTP) saklanmaz, her değişiklik bir kez görünsün.
public static class SecurityEventTypes
{
    public const string Login = "LOGIN";

    public static readonly IReadOnlyList<string> All =
    [
        Login,
        "LOGIN_ERROR",
        "LOGOUT",
        "REGISTER",
        "UPDATE_PROFILE",
        "UPDATE_EMAIL",
        "VERIFY_EMAIL",
        "SEND_RESET_PASSWORD",
        "UPDATE_CREDENTIAL",
        "UPDATE_CREDENTIAL_ERROR",
        "REMOVE_CREDENTIAL",
        "REMOVE_CREDENTIAL_ERROR",
        "DELETE_ACCOUNT",
        "USER_DISABLED_BY_TEMPORARY_LOCKOUT",
        "USER_DISABLED_BY_PERMANENT_LOCKOUT"
    ];

    public static bool IsFailure(string type) => type.EndsWith("_ERROR", StringComparison.Ordinal);
}
