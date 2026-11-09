using Gise.Identity.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Gise.Identity.Application.Auditing;

// Keycloak'taki giriş ve hesap güvenliği olaylarını denetim loguna yazar (Telgraf'taki KeycloakEventSync; bkz. tasarım 8).
// identity-audit-worker belirli aralıklarla çağırır. Her olay tek satır: kim (actor.id), hangi adresten (client.ip), ne
// (event.action, ör. "Login", "UpdateCredential") ve sonuç (event.outcome, başarısızsa Keycloak'ın hata kodu error.code).
// event.dataset "gise.audit": satır 1 yıl saklanan denetim akışına gider. Kişisel veri (e-posta, ad) yazılmaz. Var olmayan
// bir hesapla denenen girişte kişi yok: satır worker logunda kalır, adresle kaba kuvvet denemesi yine görünür.
public sealed class KeycloakEventSync
{
    // Keycloak'tan bir istekte okunan olay; bir turda en fazla MaxPagesPerRun sayfa.
    private const int PageSize = 100;
    private const int MaxPagesPerRun = 10;

    // Asıl girişi yazılan oturumlar bu süre hatırlanır; aynı oturumdaki sessiz yeniden girişler (sayfa yenileme) yazılmaz.
    // Keycloak oturumu en fazla 10 saat sürer.
    private static readonly TimeSpan SessionMemory = TimeSpan.FromHours(12);

    private readonly IKeycloakAdminClient _keycloak;
    private readonly IKeycloakEventCursor _cursor;
    private readonly TimeProvider _clock;
    private readonly ILogger<KeycloakEventSync> _logger;

    // Oturum kimliği → asıl girişin zamanı (Unix ms).
    private readonly Dictionary<string, long> _loggedSessions = [];

    public KeycloakEventSync(
        IKeycloakAdminClient keycloak,
        IKeycloakEventCursor cursor,
        TimeProvider clock,
        ILogger<KeycloakEventSync> logger)
    {
        _keycloak = keycloak;
        _cursor = cursor;
        _clock = clock;
        _logger = logger;
    }

    // Yazılan satır sayısını döndürür.
    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        var position = await _cursor.ReadAsync(cancellationToken);

        // İlk tur: imleç şu ana kurulur, öncesi geçmiş sayılır. Açılıştan önce başlamış oturumların sonraki sessiz
        // girişleri oturum başına bir kez "Login" olarak görünür.
        if (position is null)
        {
            await _cursor.WriteAsync(new KeycloakEventPosition(_clock.GetUtcNow().ToUnixTimeMilliseconds(), new HashSet<string>()), cancellationToken);
            return 0;
        }

        var fresh = await ReadNewEventsAsync(position, cancellationToken);
        if (fresh.Count == 0)
            return 0;

        ForgetOldSessions();

        var written = 0;
        foreach (var securityEvent in fresh)
        {
            if (LoginSessions.IsRepeatedLogin(securityEvent, sessionId => _loggedSessions.TryAdd(sessionId, securityEvent.Time)))
                continue;

            Write(securityEvent);
            written++;
        }

        var lastTime = fresh[^1].Time;
        var idsAtLastTime = fresh.Where(e => e.Time == lastTime).Select(e => e.Id).ToHashSet();
        if (lastTime == position.Time)
            idsAtLastTime.UnionWith(position.IdsAtTime);

        await _cursor.WriteAsync(new KeycloakEventPosition(lastTime, idsAtLastTime), cancellationToken);
        return written;
    }

    // İmleçten sonraki olaylar, eskiden yeniye. Keycloak yeniden eskiye ve sayfa sayfa verir; imlecin gerisine düşen olay
    // görülünce durulur. Okurken yeni olay gelirse sayfalar kayar ve aynı olay iki sayfada görünebilir: kimlikle tekilleşir.
    private async Task<List<KeycloakSecurityEvent>> ReadNewEventsAsync(KeycloakEventPosition position, CancellationToken cancellationToken)
    {
        var fresh = new Dictionary<string, KeycloakSecurityEvent>();

        for (var page = 0; page < MaxPagesPerRun; page++)
        {
            var events = await _keycloak.GetSecurityEventsAsync(
                SecurityEventTypes.All, userId: null, page * PageSize, PageSize, cancellationToken);

            var reachedCursor = false;
            foreach (var securityEvent in events)
            {
                if (securityEvent.Time < position.Time
                    || (securityEvent.Time == position.Time && position.IdsAtTime.Contains(securityEvent.Id)))
                {
                    reachedCursor = true;
                    continue;
                }

                fresh.TryAdd(securityEvent.Id, securityEvent);
            }

            if (reachedCursor || events.Count < PageSize)
                return Oldest(fresh.Values);
        }

        // Bir turda okunamayacak kadar çok olay (ör. toplu deneme): en yeni olaylar yazılır, daha eskileri atlanır.
        _logger.LogWarning("Keycloak'ta bir turda {Limit} olaydan fazlası birikti; daha eskileri denetim loguna yazılmadı.",
            PageSize * MaxPagesPerRun);
        return Oldest(fresh.Values);
    }

    private static List<KeycloakSecurityEvent> Oldest(IEnumerable<KeycloakSecurityEvent> events)
        => events.OrderBy(e => e.Time).ThenBy(e => e.Id, StringComparer.Ordinal).ToList();

    private void ForgetOldSessions()
    {
        var threshold = _clock.GetUtcNow().Subtract(SessionMemory).ToUnixTimeMilliseconds();
        foreach (var (sessionId, loggedAt) in _loggedSessions)
        {
            if (loggedAt < threshold)
                _loggedSessions.Remove(sessionId);
        }
    }

    private void Write(KeycloakSecurityEvent securityEvent)
    {
        var failed = SecurityEventTypes.IsFailure(securityEvent.Type);
        var action = ActionOf(securityEvent.Type);

        var fields = new Dictionary<string, object>
        {
            ["event.dataset"] = "gise.audit",
            ["event.provider"] = "keycloak",
            ["event.action"] = action,
            ["event.outcome"] = failed ? "failure" : "success",
            // Satırın zamanı worker'ın okuduğu an (en fazla bir tur sonra); olayın Keycloak'taki zamanı bu alanda.
            ["event.start"] = DateTimeOffset.FromUnixTimeMilliseconds(securityEvent.Time).UtcDateTime
        };

        if (!string.IsNullOrEmpty(securityEvent.UserId))
            fields["actor.id"] = securityEvent.UserId;

        if (!string.IsNullOrEmpty(securityEvent.IpAddress))
            fields["client.ip"] = securityEvent.IpAddress;

        if (!string.IsNullOrEmpty(securityEvent.Error))
            fields["error.code"] = securityEvent.Error;

        if (!string.IsNullOrEmpty(securityEvent.CredentialType))
            fields["target.credential.type"] = securityEvent.CredentialType;

        using var _ = _logger.BeginScope(fields);

        if (failed)
        {
            _logger.LogWarning("{Action:l} başarısız: {ErrorCode:l} (Keycloak).", action, securityEvent.Error ?? "unknown");
            return;
        }

        _logger.LogInformation("{Action:l} tamamlandı (Keycloak).", action);
    }

    // Denetim logundaki komut adlarıyla aynı biçim: "LOGIN_ERROR" → "Login" (sonuç event.outcome'da),
    // "UPDATE_CREDENTIAL" → "UpdateCredential".
    private static string ActionOf(string type)
    {
        var name = SecurityEventTypes.IsFailure(type) ? type[..^"_ERROR".Length] : type;
        return string.Concat(name.Split('_', StringSplitOptions.RemoveEmptyEntries)
            .Select(word => word[0] + word[1..].ToLowerInvariant()));
    }
}
