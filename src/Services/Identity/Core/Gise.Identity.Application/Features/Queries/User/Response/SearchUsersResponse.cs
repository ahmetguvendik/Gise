namespace Gise.Identity.Application.Features.Queries.User.Response;

public sealed class SearchUsersResponse
{
    public List<SearchUsersItem> Users { get; set; } = [];

    // Bu sayfadan sonra kullanıcı var mı ("Daha fazla göster").
    public bool HasMore { get; set; }
}

public sealed class SearchUsersItem
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
