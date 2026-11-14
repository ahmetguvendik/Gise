using Gise.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace Gise.Audit.Api.Authentication;

public static class Policies
{
    public const string Admin = nameof(Admin);

    // Denetim kayıtları kişisel veri içerir (kimlik, IP); yalnızca admin okur.
    public static AuthorizationBuilder AddAuditPolicies(this AuthorizationBuilder builder) => builder
        .AddPolicy(Admin, policy => policy.RequireRole(Roles.Admin));
}
