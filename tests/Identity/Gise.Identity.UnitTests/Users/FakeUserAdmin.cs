using Gise.Identity.Application.Interfaces;
using Gise.Identity.Application.Users;

namespace Gise.Identity.UnitTests.Users;

// Keycloak'taki kullanıcılar ve doğrudan verilmiş realm rolleri.
public sealed class FakeUserAdmin : IKeycloakUserAdmin
{
    public List<KeycloakUser> Users { get; } = [];
    public Dictionary<string, HashSet<string>> Roles { get; } = [];

    public Task<IReadOnlyList<KeycloakUser>> SearchUsersAsync(string search, int first, int max, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<KeycloakUser>>(Users
            .Where(u => search.Length == 0 || u.Email.Contains(search, StringComparison.OrdinalIgnoreCase))
            .OrderBy(u => u.Email, StringComparer.Ordinal)
            .Skip(first)
            .Take(max)
            .ToList());

    public Task<IReadOnlyList<string>> GetRealmRolesAsync(string userId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<string>>(Roles.GetValueOrDefault(userId)?.ToList() ?? []);

    public Task AddRealmRoleAsync(string userId, string role, CancellationToken cancellationToken = default)
    {
        (Roles.TryGetValue(userId, out var set) ? set : Roles[userId] = []).Add(role);
        return Task.CompletedTask;
    }

    public Task RemoveRealmRoleAsync(string userId, string role, CancellationToken cancellationToken = default)
    {
        Roles.GetValueOrDefault(userId)?.Remove(role);
        return Task.CompletedTask;
    }
}
