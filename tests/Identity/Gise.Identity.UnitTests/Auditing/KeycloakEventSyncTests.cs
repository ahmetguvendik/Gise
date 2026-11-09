using Gise.Identity.Application.Auditing;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Logging;
using static Gise.Identity.UnitTests.FakeKeycloak;

namespace Gise.Identity.UnitTests.Auditing;

// Keycloak olaylarının denetim satırına dönüşmesi: alanlar, tekrar eden girişler, imleç ve sayfalama.
public sealed class KeycloakEventSyncTests
{
    private sealed class CollectingSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = [];

        public void Emit(LogEvent logEvent) => Events.Add(logEvent);
    }

    private static readonly DateTimeOffset Start = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);

    private readonly CollectingSink _sink = new();
    private readonly FakeKeycloak _keycloak = new();
    private readonly MutableTimeProvider _clock = new(Start);
    private readonly KeycloakEventSync _sync;

    public KeycloakEventSyncTests()
    {
        var logger = new LoggerConfiguration().Enrich.FromLogContext().WriteTo.Sink(_sink).CreateLogger();
        var factory = new SerilogLoggerFactory(logger, dispose: true);
        _sync = new KeycloakEventSync(_keycloak, new InMemoryKeycloakEventCursor(), _clock, factory.CreateLogger<KeycloakEventSync>());
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static long At(int seconds) => Start.AddSeconds(seconds).ToUnixTimeMilliseconds();

    private static string? Field(LogEvent logEvent, string name)
        => logEvent.Properties.TryGetValue(name, out var value) ? (value as ScalarValue)?.Value?.ToString() : null;

    [Fact]
    public async Task First_run_only_sets_the_cursor()
    {
        _keycloak.Events.Add(Event(At(-60), "LOGIN", session: "old"));

        Assert.Equal(0, await _sync.RunAsync(Ct));
        Assert.Empty(_sink.Events);
    }

    [Fact]
    public async Task Writes_new_events_as_audit_lines_oldest_first()
    {
        await _sync.RunAsync(Ct);
        _keycloak.Events.Add(Event(At(2), "UPDATE_CREDENTIAL", session: "s1", credentialType: "otp"));
        _keycloak.Events.Add(Event(At(1), "LOGIN_ERROR", error: "invalid_user_credentials"));

        Assert.Equal(2, await _sync.RunAsync(Ct));

        var failed = _sink.Events[0];
        Assert.Equal(LogEventLevel.Warning, failed.Level);
        Assert.Equal("gise.audit", Field(failed, "event.dataset"));
        Assert.Equal("keycloak", Field(failed, "event.provider"));
        Assert.Equal("Login", Field(failed, "event.action"));
        Assert.Equal("failure", Field(failed, "event.outcome"));
        Assert.Equal("invalid_user_credentials", Field(failed, "error.code"));
        Assert.Equal("user-1", Field(failed, "actor.id"));
        Assert.Equal("203.0.113.7", Field(failed, "client.ip"));

        var otp = _sink.Events[1];
        Assert.Equal("UpdateCredential", Field(otp, "event.action"));
        Assert.Equal("success", Field(otp, "event.outcome"));
        Assert.Equal("otp", Field(otp, "target.credential.type"));
    }

    [Fact]
    public async Task Repeated_logins_of_a_session_are_written_once()
    {
        await _sync.RunAsync(Ct);
        _keycloak.Events.Add(Event(At(1), "LOGIN", session: "s1"));
        await _sync.RunAsync(Ct);

        _keycloak.Events.Add(Event(At(5), "LOGIN", session: "s1"));
        _keycloak.Events.Add(Event(At(6), "LOGIN", session: "s2"));
        await _sync.RunAsync(Ct);

        Assert.Equal(2, _sink.Events.Count);
        Assert.All(_sink.Events, e => Assert.Equal("Login", Field(e, "event.action")));
    }

    [Fact]
    public async Task Event_at_the_cursor_millisecond_is_not_written_twice_but_a_late_one_is()
    {
        await _sync.RunAsync(Ct);
        _keycloak.Events.Add(Event(At(1), "LOGOUT", session: "a"));
        await _sync.RunAsync(Ct);

        _keycloak.Events.Add(Event(At(1), "LOGOUT", session: "b"));
        await _sync.RunAsync(Ct);
        await _sync.RunAsync(Ct);

        Assert.Equal(2, _sink.Events.Count);
    }

    [Fact]
    public async Task Reads_further_pages_until_it_reaches_the_cursor()
    {
        await _sync.RunAsync(Ct);
        for (var i = 1; i <= 250; i++)
            _keycloak.Events.Add(Event(At(i), "LOGIN_ERROR", error: "invalid_user_credentials"));

        Assert.Equal(250, await _sync.RunAsync(Ct));
        // İlk tur Keycloak'a gitmez; 100 + 100 + 50: dolmayan üçüncü sayfada durur.
        Assert.Equal(3, _keycloak.Requests);
    }

    [Fact]
    public async Task Unknown_user_has_no_actor_and_no_personal_data()
    {
        await _sync.RunAsync(Ct);
        _keycloak.Events.Add(Event(At(1), "LOGIN_ERROR", userId: null, error: "user_not_found"));

        await _sync.RunAsync(Ct);

        var line = Assert.Single(_sink.Events);
        Assert.False(line.Properties.ContainsKey("actor.id"));
        Assert.DoesNotContain(line.Properties.Keys, key => key.Contains("user", StringComparison.OrdinalIgnoreCase)
                                                          || key.Contains("email", StringComparison.OrdinalIgnoreCase));
    }
}
