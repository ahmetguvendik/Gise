namespace Gise.Authentication;

// Keycloak realm rolleriyle birebir aynı adlar (deploy/keycloak/gise-realm.json); ön yüzdeki karşılığı
// src/Client/src/auth/roles.ts. Rol adı değişirse üçü birlikte değişir.
public static class Roles
{
    public const string Customer = "customer";
    public const string Organizer = "organizer";
    public const string GateStaff = "gate-staff";
    public const string Admin = "admin";
}
