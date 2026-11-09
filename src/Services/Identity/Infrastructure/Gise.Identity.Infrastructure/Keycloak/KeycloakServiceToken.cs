using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Gise.Exceptions;
using Microsoft.Extensions.Configuration;

namespace Gise.Identity.Infrastructure.Keycloak;

// gise-identity servis hesabının token'ı (client credentials). Süresi bitmeden 30 sn önce yenilenir; her çağrıda yeni token
// istenmez. Gizli anahtar compose'dan ortam değişkeniyle gelir (deploy/.env, IDENTITY_CLIENT_SECRET).
public sealed class KeycloakServiceToken
{
    private static readonly TimeSpan RenewBefore = TimeSpan.FromSeconds(30);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TimeProvider _clock;
    private readonly string _tokenUrl;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private string? _token;
    private DateTimeOffset _renewAt;

    public KeycloakServiceToken(IHttpClientFactory httpClientFactory, TimeProvider clock, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _clock = clock;

        var keycloak = configuration.GetRequiredSection("Keycloak");
        string Required(string key) => keycloak[key] is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"Keycloak:{key} ayarı eksik.");

        _tokenUrl = $"{Required("InternalAuthority")}/protocol/openid-connect/token";
        _clientId = Required("ServiceClientId");
        _clientSecret = Required("ServiceClientSecret");
    }

    public async Task<string> GetAsync(CancellationToken cancellationToken)
    {
        if (_token is { } cached && _clock.GetUtcNow() < _renewAt)
            return cached;

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_token is { } current && _clock.GetUtcNow() < _renewAt)
                return current;

            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _clientId,
                ["client_secret"] = _clientSecret
            });

            HttpResponseMessage response;
            try
            {
                response = await _httpClientFactory.CreateClient(ServiceRegistration.HttpClientName)
                    .PostAsync(_tokenUrl, content, cancellationToken);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
            {
                throw ServiceUnavailableException.Keycloak($"Servis hesabı token'ı alınamadı: {ex.Message}");
            }

            using (response)
            {
                // 401: gizli anahtar Keycloak'takiyle aynı değil (bkz. deploy/.env IDENTITY_CLIENT_SECRET).
                if (!response.IsSuccessStatusCode)
                    throw ServiceUnavailableException.Keycloak($"Servis hesabı token'ı alınamadı (HTTP {(int)response.StatusCode}).");

                var token = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken)
                    ?? throw ServiceUnavailableException.Keycloak("Token yanıtı boş.");

                _token = token.AccessToken;
                _renewAt = _clock.GetUtcNow().AddSeconds(token.ExpiresIn).Subtract(RenewBefore);
                return _token;
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    // Keycloak token'ı reddettiyse (ör. anahtar yenilendi) bir sonraki çağrı yenisini alır.
    public void Invalidate() => _token = null;

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}
