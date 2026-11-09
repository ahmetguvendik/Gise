namespace Gise.Identity.Application.Users;

// Uygulamadan verilip alınabilen realm rolleri. Adlar Keycloak'taki ve Gise.Authentication.Roles'teki ile aynı;
// "customer" herkesin varsayılan rolüdür (default-roles-gise), buradan yönetilmez.
public static class AssignableRoles
{
    public const string Organizer = "organizer";
    public const string GateStaff = "gate-staff";
    public const string Admin = "admin";

    public static readonly IReadOnlyList<string> All = [Organizer, GateStaff, Admin];

    public static bool Contains(string role) => All.Contains(role, StringComparer.Ordinal);
}
