using System.Net;
using System.Net.Http.Json;
using Gise.Exceptions;
using Gise.Identity.Application.Interfaces;
using Gise.Identity.Application.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gise.Identity.Infrastructure.Keycloak;

// Kullanıcı arama ve realm rolü verme/alma, gise-identity-roles servis hesabıyla (query-users, view-users,
// manage-users). Keycloak bu hesaba kendi yönetici rollerini (realm-admin) verdirmez; denendi, 403. Rolün kimliği
// rol uçlarından değil (view-realm ister) kullanıcının atanabilir/atanmış rol listesinden alınır.
public sealed class KeycloakUserAdmin : IKeycloakUserAdmin
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly KeycloakServiceToken _token;
    private readonly string _adminUrl;

    public KeycloakUserAdmin(
        IHttpClientFactory httpClientFactory,
        [FromKeyedServices(ServiceRegistration.RolesAccount)] KeycloakServiceToken token,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _token = token;
        _adminUrl = configuration.GetRequiredSection("Keycloak")["AdminUrl"] is { Length: > 0 } url
            ? url
            : throw new InvalidOperationException("Keycloak:AdminUrl ayarı eksik.");
    }

    // Servis hesapları listede yer almaz (Keycloak varsayılanı). Keycloak'ın araması varsayılan olarak baştan eşleşir
    // ("gise", "admin@gise.local"u bulmaz); *…* ile içinde geçen aranır. Kullanıcının yazdığı * ve " atılır, aramanın
    // biçimini değiştirmesin.
    public async Task<IReadOnlyList<KeycloakUser>> SearchUsersAsync(string search, int first, int max, CancellationToken cancellationToken = default)
    {
        var term = search.Replace("*", string.Empty).Replace("\"", string.Empty);
        var filter = term.Length == 0 ? string.Empty : $"search={Uri.EscapeDataString($"*{term}*")}&";
        var users = await GetAsync<List<UserRepresentation>>(
            $"/users?{filter}first={first}&max={max}&briefRepresentation=true", userId: null, cancellationToken);

        return users
            .Where(u => !string.IsNullOrEmpty(u.Id))
            .Select(u => new KeycloakUser(u.Id!, u.Email ?? u.Username ?? string.Empty, u.FirstName, u.LastName, u.Enabled, u.EmailVerified))
            .ToList();
    }

    public async Task<IReadOnlyList<string>> GetRealmRolesAsync(string userId, CancellationToken cancellationToken = default)
        => (await GetMappedRolesAsync(userId, cancellationToken)).Select(r => r.Name).ToList();

    public async Task AddRealmRoleAsync(string userId, string role, CancellationToken cancellationToken = default)
    {
        var available = await GetAsync<List<RoleRepresentation>>($"/users/{Escape(userId)}/role-mappings/realm/available", userId, cancellationToken);
        var match = available.FirstOrDefault(r => r.Name == role);

        // Atanabilirler arasında yoksa ya zaten var ya da realm'de böyle bir rol yok (adlar AssignableRoles'ta sabit).
        if (match is null)
        {
            if ((await GetMappedRolesAsync(userId, cancellationToken)).Any(r => r.Name == role))
                return;

            throw new InvalidOperationException($"Keycloak'ta '{role}' realm rolü yok.");
        }

        await SendRoleMappingAsync(HttpMethod.Post, userId, match, cancellationToken);
    }

    public async Task RemoveRealmRoleAsync(string userId, string role, CancellationToken cancellationToken = default)
    {
        var match = (await GetMappedRolesAsync(userId, cancellationToken)).FirstOrDefault(r => r.Name == role);
        if (match is null)
            return;

        await SendRoleMappingAsync(HttpMethod.Delete, userId, match, cancellationToken);
    }

    private Task<List<RoleRepresentation>> GetMappedRolesAsync(string userId, CancellationToken cancellationToken)
        => GetAsync<List<RoleRepresentation>>($"/users/{Escape(userId)}/role-mappings/realm", userId, cancellationToken);

    private async Task SendRoleMappingAsync(HttpMethod method, string userId, RoleRepresentation role, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, $"{_adminUrl}/users/{Escape(userId)}/role-mappings/realm")
        {
            Content = JsonContent.Create(new[] { role })
        };
        using var response = await KeycloakAdminHttp.SendAsync(_httpClientFactory, _token, request, cancellationToken);
        EnsureSuccess(response, userId);
    }

    private async Task<T> GetAsync<T>(string path, string? userId, CancellationToken cancellationToken) where T : new()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, _adminUrl + path);
        using var response = await KeycloakAdminHttp.SendAsync(_httpClientFactory, _token, request, cancellationToken);
        EnsureSuccess(response, userId);
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken) ?? new T();
    }

    private static void EnsureSuccess(HttpResponseMessage response, string? userId)
    {
        if (response.StatusCode == HttpStatusCode.NotFound && userId is not null)
            throw NotFoundException.User(userId);

        if (!response.IsSuccessStatusCode)
            throw ServiceUnavailableException.Keycloak($"Keycloak yönetim API'si HTTP {(int)response.StatusCode} döndü.");
    }

    private static string Escape(string userId) => Uri.EscapeDataString(userId);

    private sealed record UserRepresentation(
        string? Id,
        string? Username,
        string? Email,
        string? FirstName,
        string? LastName,
        bool Enabled,
        bool EmailVerified);

    // Rol eşlemesi gönderilirken Keycloak kimlik ve adı birlikte ister.
    private sealed record RoleRepresentation(string Id, string Name);
}
