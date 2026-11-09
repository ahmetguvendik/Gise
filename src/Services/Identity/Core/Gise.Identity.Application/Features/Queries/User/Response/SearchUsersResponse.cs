namespace Gise.Identity.Application.Features.Queries.User.Response;

public sealed class SearchUsersResponse
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool Enabled { get; set; }
    public bool EmailVerified { get; set; }

    // Uygulamadan yönetilen rollerden kullanıcıda olanlar: organizer, gate-staff, admin.
    public List<string> Roles { get; set; } = [];
}
