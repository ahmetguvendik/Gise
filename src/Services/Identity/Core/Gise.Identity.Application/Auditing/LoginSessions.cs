namespace Gise.Identity.Application.Auditing;

// Token yalnızca bellekte durduğu için sayfa her yenilendiğinde uygulama Keycloak'a sessizce yeniden girer (prompt=none);
// Keycloak bunu da LOGIN olarak yazar ve parolayla girişten ayırt edilemez (ayrıntılar aynı). Ayıran oturum kimliği:
// oturumun ilk LOGIN'i asıl giriştir, aynı oturumdaki sonrakiler sayılmaz.
public static class LoginSessions
{
    // Keycloak olayları yeniden eskiye verir; sonuç da yeniden eskiye.
    public static IReadOnlyList<KeycloakSecurityEvent> WithoutRepeatedLogins(IReadOnlyList<KeycloakSecurityEvent> newestFirst)
    {
        var seenSessions = new HashSet<string>();
        var kept = new List<KeycloakSecurityEvent>(newestFirst.Count);

        for (var i = newestFirst.Count - 1; i >= 0; i--)
        {
            var securityEvent = newestFirst[i];
            if (IsRepeatedLogin(securityEvent, seenSessions.Add))
                continue;

            kept.Add(securityEvent);
        }

        kept.Reverse();
        return kept;
    }

    // remember: oturum ilk kez görülüyorsa true döner ve oturumu hatırlar.
    public static bool IsRepeatedLogin(KeycloakSecurityEvent securityEvent, Func<string, bool> remember)
        => securityEvent.Type == SecurityEventTypes.Login
           && securityEvent.SessionId is { Length: > 0 } sessionId
           && !remember(sessionId);
}
