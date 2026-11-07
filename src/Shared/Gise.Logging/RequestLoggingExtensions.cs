using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;

namespace Gise.Logging;

public static class RequestLoggingExtensions
{
    // İstek logu (bkz. tasarım 8): API'ye gelen her istek için tek satır; kim ve nereden (actor.*, client.ip) UserEnricher'dan.
    // ASP.NET Core'un kendi istek logları Warning'de kalır: istek başına birkaç satır yazarlar. Adres sorgu dizesiz yazılır:
    // koltuk haritası websocket'inde token sorgu dizesinde. Sağlık yoklamaları yazılmaz. Pipeline'ın başında çağrılır.
    public static IApplicationBuilder UseAppRequestLogging(this IApplicationBuilder app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "{RequestMethod:l} {RequestPath:l} → {StatusCode} ({Elapsed:0} ms)";

            options.GetLevel = (context, _, exception) =>
                context.Request.Path.StartsWithSegments("/health") ? LogEventLevel.Verbose
                : exception is not null || context.Response.StatusCode >= 500 ? LogEventLevel.Error
                : context.Response.StatusCode >= 400 ? LogEventLevel.Warning
                : LogEventLevel.Information;
        });

        // Hangi uç: yoldaki kimlikler yerine şablon ("api/orders/{id:guid}"); Kibana'da uca göre gruplanır. İstek bitince
        // değil başında yazılır: hata yanıtında UseExceptionHandler seçilen ucu temizliyor. Uç, WebApplication'ın en başa
        // koyduğu routing'de seçilmiş olur.
        var diagnosticContext = app.ApplicationServices.GetRequiredService<IDiagnosticContext>();
        return app.Use((context, next) =>
        {
            if (context.GetEndpoint() is RouteEndpoint endpoint)
                diagnosticContext.Set("http.route", endpoint.RoutePattern.RawText ?? endpoint.DisplayName);

            return next(context);
        });
    }
}
