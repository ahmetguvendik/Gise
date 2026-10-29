using Gise.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace Gise.Catalog.Api.Authentication;

public static class Policies
{
    public const string Organizer = nameof(Organizer);
    public const string VenueManager = nameof(VenueManager);
    public const string EventModerator = nameof(EventModerator);

    public static AuthorizationBuilder AddCatalogPolicies(this AuthorizationBuilder builder) => builder
        .AddPolicy(Organizer, policy => policy.RequireRole(Roles.Organizer, Roles.Admin))
        // Mekânlar platformun ortak verisi; organizatör yalnızca var olan mekânlarda etkinlik açar.
        .AddPolicy(VenueManager, policy => policy.RequireRole(Roles.Admin))
        // Organizatör taslak hazırlar ve onaya gönderir; yayınlama, geri gönderme ve iptal admin'in işi.
        .AddPolicy(EventModerator, policy => policy.RequireRole(Roles.Admin));
}
