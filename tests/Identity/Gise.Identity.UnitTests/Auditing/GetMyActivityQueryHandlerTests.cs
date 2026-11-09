using Gise.Identity.Application.Features.Handlers.Account.QueryHandlers;
using Gise.Identity.Application.Features.Queries.Account.Request;
using static Gise.Identity.UnitTests.FakeKeycloak;

namespace Gise.Identity.UnitTests.Auditing;

public sealed class GetMyActivityQueryHandlerTests
{
    [Fact]
    public async Task Returns_only_the_users_events_newest_first_without_silent_logins()
    {
        var keycloak = new FakeKeycloak();
        keycloak.Events.Add(Event(1_000, "LOGIN", session: "s1"));
        keycloak.Events.Add(Event(2_000, "LOGIN", session: "s1"));
        keycloak.Events.Add(Event(3_000, "UPDATE_CREDENTIAL", session: "s1", credentialType: "password"));
        keycloak.Events.Add(Event(4_000, "LOGIN_ERROR", error: "invalid_user_credentials"));
        keycloak.Events.Add(Event(5_000, "LOGIN", userId: "someone-else", session: "x"));

        var result = await new GetMyActivityQueryHandler(keycloak)
            .Handle(new GetMyActivityQuery { UserId = "user-1" }, TestContext.Current.CancellationToken);

        Assert.Equal(["LOGIN_ERROR", "UPDATE_CREDENTIAL", "LOGIN"], result.Select(r => r.Type));
        Assert.True(result[0].Failed);
        Assert.Equal("password", result[1].CredentialType);
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1_000).UtcDateTime, result[2].Time);
    }
}
