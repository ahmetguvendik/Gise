using Gise.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace Gise.HealthCheck.Api.Authentication;

public static class Policies
{
    public const string Admin = nameof(Admin);

    // Sistem durumu yalnızca admin'in: servis adları, hata metinleri ve altyapı adresleri saldırgana keşif verir.
    public static AuthorizationBuilder AddHealthCheckPolicies(this AuthorizationBuilder builder) => builder
        .AddPolicy(Admin, policy => policy.RequireRole(Roles.Admin));
}
