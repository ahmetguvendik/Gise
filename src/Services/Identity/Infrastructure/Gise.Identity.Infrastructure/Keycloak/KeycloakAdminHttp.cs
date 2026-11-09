using System.Net;
using System.Net.Http.Headers;
using Gise.Exceptions;

namespace Gise.Identity.Infrastructure.Keycloak;

// Keycloak yönetim API'sine istek: servis hesabının token'ı eklenir; ulaşılamazsa ya da sunucu hatası dönerse 503
// (ServiceUnavailableException). 401'de token bir sonraki istekte yenilenir. Diğer durum kodlarını çağıran yorumlar.
internal static class KeycloakAdminHttp
{
    public static async Task<HttpResponseMessage> SendAsync(
        IHttpClientFactory httpClientFactory,
        KeycloakServiceToken token,
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await token.GetAsync(cancellationToken));

        HttpResponseMessage response;
        try
        {
            response = await httpClientFactory.CreateClient(ServiceRegistration.HttpClientName).SendAsync(request, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            throw ServiceUnavailableException.Keycloak($"Keycloak yönetim API'sine ulaşılamadı: {ex.Message}");
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            token.Invalidate();

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden || (int)response.StatusCode >= 500)
        {
            var status = (int)response.StatusCode;
            response.Dispose();
            throw ServiceUnavailableException.Keycloak($"Keycloak yönetim API'si {request.Method} {request.RequestUri?.AbsolutePath} isteğine HTTP {status} döndü.");
        }

        return response;
    }
}
