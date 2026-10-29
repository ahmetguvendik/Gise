using Gise.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace Gise.Ticket.Api.Authentication;

public static class Policies
{
    public const string GateStaff = nameof(GateStaff);

    // Kapıda okutma görevlinin ya da admin'in işi.
    public static AuthorizationBuilder AddTicketPolicies(this AuthorizationBuilder builder) => builder
        .AddPolicy(GateStaff, policy => policy.RequireRole(Roles.GateStaff, Roles.Admin));
}
