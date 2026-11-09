using Gise.Identity.Application.Auditing;
using Gise.Identity.Application.Interfaces;

namespace Gise.Identity.UnitTests;

// Keycloak'ın olay listesi gibi: yeniden eskiye, first/max ile sayfalı, user ile süzülür.
public sealed class FakeKeycloak : IKeycloakAdminClient
{
    public List<KeycloakSecurityEvent> Events { get; } = [];

    public int Requests { get; private set; }

    public Task<IReadOnlyList<KeycloakSecurityEvent>> GetSecurityEventsAsync(
        IReadOnlyCollection<string> types, string? userId, int first, int max, CancellationToken cancellationToken = default)
    {
        Requests++;
        IReadOnlyList<KeycloakSecurityEvent> page = Events
            .Where(e => types.Contains(e.Type) && (userId is null || e.UserId == userId))
            .OrderByDescending(e => e.Time)
            .Skip(first)
            .Take(max)
            .ToList();
        return Task.FromResult(page);
    }

    public static KeycloakSecurityEvent Event(long time, string type, string? userId = "user-1", string? session = null,
        string? error = null, string? credentialType = null) => new()
    {
        Id = $"{time}-{type}-{session}-{Guid.NewGuid():N}",
        Time = time,
        Type = type,
        UserId = userId,
        SessionId = session,
        IpAddress = "203.0.113.7",
        ClientId = "gise-web",
        Error = error,
        CredentialType = credentialType
    };
}

public sealed class MutableTimeProvider(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;
}
