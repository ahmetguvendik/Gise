using Gise.Exceptions;
using Gise.Identity.Application.Features.Commands.User.Request;
using Gise.Identity.Application.Features.Handlers.User.CommandHandlers;
using Gise.Identity.Application.Features.Handlers.User.QueryHandlers;
using Gise.Identity.Application.Features.Queries.User.Request;
using Gise.Identity.Application.Users;
using Gise.Identity.Application.Validators;

namespace Gise.Identity.UnitTests.Users;

public sealed class RoleManagementTests
{
    private const string Admin = "11111111-1111-1111-1111-111111111111";
    private const string Other = "22222222-2222-2222-2222-222222222222";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly FakeUserAdmin _keycloak = new();

    [Fact]
    public async Task Admin_cannot_revoke_their_own_admin_role()
    {
        _keycloak.Roles[Admin] = [AssignableRoles.Admin];
        var handler = new RevokeRoleCommandHandler(_keycloak);

        var error = await Assert.ThrowsAsync<BusinessException>(() =>
            handler.Handle(new RevokeRoleCommand { UserId = Admin, Role = AssignableRoles.Admin, RequestedBy = Admin }, Ct));

        Assert.Equal(ErrorCodes.User.CannotRevokeOwnAdmin, error.ErrorCode);
        Assert.Contains(AssignableRoles.Admin, _keycloak.Roles[Admin]);
    }

    [Fact]
    public async Task Admin_can_revoke_another_admin_and_their_own_other_roles()
    {
        _keycloak.Roles[Other] = [AssignableRoles.Admin];
        _keycloak.Roles[Admin] = [AssignableRoles.Admin, AssignableRoles.Organizer];
        var handler = new RevokeRoleCommandHandler(_keycloak);

        await handler.Handle(new RevokeRoleCommand { UserId = Other, Role = AssignableRoles.Admin, RequestedBy = Admin }, Ct);
        await handler.Handle(new RevokeRoleCommand { UserId = Admin, Role = AssignableRoles.Organizer, RequestedBy = Admin }, Ct);

        Assert.Empty(_keycloak.Roles[Other]);
        Assert.Equal([AssignableRoles.Admin], _keycloak.Roles[Admin]);
    }

    [Fact]
    public async Task Grant_adds_the_role()
    {
        await new GrantRoleCommandHandler(_keycloak)
            .Handle(new GrantRoleCommand { UserId = Other, Role = AssignableRoles.GateStaff, RequestedBy = Admin }, Ct);

        Assert.Contains(AssignableRoles.GateStaff, _keycloak.Roles[Other]);
    }

    [Theory]
    [InlineData("organizer", true)]
    [InlineData("gate-staff", true)]
    [InlineData("admin", true)]
    [InlineData("customer", false)]
    [InlineData("realm-admin", false)]
    [InlineData("default-roles-gise", false)]
    [InlineData("Admin", false)]
    public void Only_app_roles_can_be_granted(string role, bool valid)
    {
        var result = new GrantRoleCommandValidator().Validate(new GrantRoleCommand { UserId = Other, Role = role, RequestedBy = Admin });

        Assert.Equal(valid, result.IsValid);
    }

    [Theory]
    [InlineData("../../roles/admin")]
    [InlineData("not-a-guid")]
    [InlineData("")]
    public void User_id_must_be_a_keycloak_uuid(string userId)
    {
        var result = new RevokeRoleCommandValidator().Validate(new RevokeRoleCommand { UserId = userId, Role = "organizer", RequestedBy = Admin });

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Search_returns_only_app_roles()
    {
        _keycloak.Users.Add(new KeycloakUser(Other, "kapi@gise.local", "Kapı", "Görevlisi", true, true));
        _keycloak.Roles[Other] = ["default-roles-gise", AssignableRoles.GateStaff, "offline_access"];

        var result = await new SearchUsersQueryHandler(_keycloak).Handle(new SearchUsersQuery { Search = " kapi " }, Ct);

        var user = Assert.Single(result);
        Assert.Equal([AssignableRoles.GateStaff], user.Roles);
        Assert.Equal("kapi@gise.local", user.Email);
    }
}
