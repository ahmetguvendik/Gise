using System.Net.Http.Json;
using Gise.Exceptions;
using Gise.Identity.Application.Auditing;
using Gise.Identity.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gise.Identity.Infrastructure.Keycloak;

// Keycloak'ın yönetim API'si, konteyner ağından (keycloak:8080; gateway'den yayınlanmaz). Ulaşılamazsa ya da hata dönerse
// 503 (ServiceUnavailableException).
public sealed class KeycloakAdminClient : IKeycloakAdminClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly KeycloakServiceToken _token;
    private readonly string _adminUrl;

    public KeycloakAdminClient(
        IHttpClientFactory httpClientFactory,
        [FromKeyedServices(ServiceRegistration.EventsAccount)] KeycloakServiceToken token,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _token = token;
        _adminUrl = configuration.GetRequiredSection("Keycloak")["AdminUrl"] is { Length: > 0 } url
            ? url
            : throw new InvalidOperationException("Keycloak:AdminUrl ayarı eksik.");
    }

    public async Task<IReadOnlyList<KeycloakSecurityEvent>> GetSecurityEventsAsync(
        IReadOnlyCollection<string> types,
        string? userId,
        int first,
        int max,
        CancellationToken cancellationToken = default)
    {
        var query = new List<string> { $"first={first}", $"max={max}" };
        query.AddRange(types.Select(type => $"type={Uri.EscapeDataString(type)}"));
        if (!string.IsNullOrEmpty(userId))
            query.Add($"user={Uri.EscapeDataString(userId)}");

        using var request = new HttpRequestMessage(HttpMethod.Get, $"{_adminUrl}/events?{string.Join('&', query)}");
        using var response = await KeycloakAdminHttp.SendAsync(_httpClientFactory, _token, request, cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw ServiceUnavailableException.Keycloak($"Keycloak olayları okunamadı (HTTP {(int)response.StatusCode}).");

        var events = await response.Content.ReadFromJsonAsync<List<EventRepresentation>>(cancellationToken) ?? [];
        return events.Select(ToSecurityEvent).ToList();
    }

    private static KeycloakSecurityEvent ToSecurityEvent(EventRepresentation e) => new()
    {
        Id = e.Id ?? $"{e.Time}:{e.Type}:{e.SessionId}",
        Time = e.Time,
        Type = e.Type ?? string.Empty,
        UserId = e.UserId,
        SessionId = e.SessionId,
        IpAddress = e.IpAddress,
        ClientId = e.ClientId,
        Error = e.Error,
        CredentialType = e.Details?.GetValueOrDefault("credential_type")
    };

    // Keycloak'ın EventRepresentation'ı; ayrıntılardan (details) yalnızca kimlik bilgisi türü okunur. E-posta, kullanıcı adı
    // gibi kişisel ayrıntılar alınmaz.
    private sealed record EventRepresentation(
        string? Id,
        long Time,
        string? Type,
        string? UserId,
        string? SessionId,
        string? IpAddress,
        string? ClientId,
        string? Error,
        Dictionary<string, string>? Details);
}
