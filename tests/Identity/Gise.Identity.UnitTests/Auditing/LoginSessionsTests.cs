using Gise.Identity.Application.Auditing;
using static Gise.Identity.UnitTests.FakeKeycloak;

namespace Gise.Identity.UnitTests.Auditing;

// Sayfa yenilemelerinin sessiz girişleri (aynı oturumda tekrar eden LOGIN) sayılmaz; asıl giriş oturumun ilkidir.
public sealed class LoginSessionsTests
{
    [Fact]
    public void Keeps_only_the_first_login_of_each_session()
    {
        var newestFirst = new[]
        {
            Event(500, "LOGIN", session: "a"),
            Event(400, "UPDATE_CREDENTIAL", session: "a", credentialType: "password"),
            Event(300, "LOGIN", session: "b"),
            Event(200, "LOGIN", session: "a"),
            Event(100, "LOGIN_ERROR", error: "invalid_user_credentials")
        };

        var kept = LoginSessions.WithoutRepeatedLogins(newestFirst);

        Assert.Equal([400L, 300L, 200L, 100L], kept.Select(e => e.Time));
    }

    [Fact]
    public void Login_without_session_is_kept()
    {
        var kept = LoginSessions.WithoutRepeatedLogins([Event(200, "LOGIN"), Event(100, "LOGIN")]);

        Assert.Equal(2, kept.Count);
    }
}
