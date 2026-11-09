using Gise.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace Gise.Identity.Api.Authentication;

public static class Policies
{
    public const string Admin = nameof(Admin);

    // Kullanıcı arama ve rol verme/alma yalnızca admin'in işi.
    public static AuthorizationBuilder AddIdentityPolicies(this AuthorizationBuilder builder) => builder
        .AddPolicy(Admin, policy => policy.RequireRole(Roles.Admin));
}
