using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;

namespace Gise.Logging;

// Yalnızca kimlik (Keycloak sub) yazılır; ad ve e-posta kişisel veri olduğu için loga girmez.
public sealed class UserEnricher : ILogEventEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserEnricher(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context is null)
            return;

        // Kimlikten önce: adrese en çok anonim isteklerde (hız sınırı, başarısız deneme) ihtiyaç var.
        var clientIp = ResolveClientIp(context);
        if (!string.IsNullOrEmpty(clientIp))
            logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("client.ip", clientIp));

        if (context.User.Identity?.IsAuthenticated != true)
            return;

        var subject = context.User.FindFirst("sub")?.Value;
        if (!string.IsNullOrEmpty(subject))
            logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("actor.id", subject));
    }

    // APISIX arkasında RemoteIpAddress hep gateway'i gösterir; gerçek adres başlıkta. Başlık uydurulabilir: yalnızca
    // loglama için, yetki kararında kullanılmaz.
    private static string? ResolveClientIp(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
            return forwardedFor.Split(',')[0].Trim();

        var realIp = context.Request.Headers["X-Real-IP"].ToString();
        if (!string.IsNullOrWhiteSpace(realIp))
            return realIp.Trim();

        return context.Connection.RemoteIpAddress?.ToString();
    }
}
