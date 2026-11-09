using System.Diagnostics;
using Gise.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using Serilog.Core;
using Serilog.Events;

namespace Gise.Identity.Application.Behaviors;

// "Kim ne yaptı" kaydı (bkz. tasarım 8; diğer servislerdeki CommandLoggingBehavior'ın veritabanısız hâli). Her command
// bitince tek satır: kim (actor.id ve actor.roles istekten), ne (event.action, ör. "GrantRole"), kime (target.user.id),
// hangi rol (target.role.name) ve sonuç. Başarısız denemeler de yazılır (event.outcome failure, error.code).
// event.dataset "gise.audit": satır 1 yıl saklanan denetim akışına gider. Query'ler yazılmaz. ValidationBehavior'dan önce
// kayıtlı: doğrulamaya takılan deneme de görünür.
public sealed class CommandLoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<CommandLoggingBehavior<TRequest, TResponse>> _logger;

    public CommandLoggingBehavior(ILogger<CommandLoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var name = typeof(TRequest).Name;
        if (!name.EndsWith("Command", StringComparison.Ordinal))
            return await next(cancellationToken);

        var action = name[..^"Command".Length];
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next(cancellationToken);

            using (LogContext.Push(new AuditFieldsEnricher(action, request, errorCode: null)))
                _logger.LogInformation("{Command:l} tamamlandı ({ElapsedMs} ms).", action, stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            var (errorCode, statusCode) = ex is AppException appException
                ? (appException.ErrorCode, appException.StatusCode)
                : (ErrorCodes.General.InternalError, 500);

            // StatusCode: istemcinin aldığı yanıt (Logstash http.response.status_code yapar).
            using (LogContext.Push(new AuditFieldsEnricher(action, request, errorCode)))
                _logger.LogWarning("{Command:l} başarısız: {ErrorCode:l} ({StatusCode}, {ElapsedMs} ms).",
                    action, errorCode, statusCode, stopwatch.ElapsedMilliseconds);

            throw;
        }
    }

    private sealed class AuditFieldsEnricher : ILogEventEnricher
    {
        private readonly string _action;
        private readonly object _request;
        private readonly string? _errorCode;

        public AuditFieldsEnricher(string action, object request, string? errorCode)
        {
            _action = action;
            _request = request;
            _errorCode = errorCode;
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory factory)
        {
            logEvent.AddPropertyIfAbsent(factory.CreateProperty("event.dataset", "gise.audit"));
            logEvent.AddPropertyIfAbsent(factory.CreateProperty("event.action", _action));
            logEvent.AddPropertyIfAbsent(factory.CreateProperty("event.outcome", _errorCode is null ? "success" : "failure"));

            if (_errorCode is not null)
                logEvent.AddPropertyIfAbsent(factory.CreateProperty("error.code", _errorCode));

            // Yalnızca kimlik ve rol adı: e-posta ve ad command'da yok, loga da girmez. İşlemi yapan (RequestedBy) zaten
            // actor.id'de.
            foreach (var property in _request.GetType().GetProperties())
            {
                var field = property.Name switch
                {
                    "UserId" => "target.user.id",
                    "Role" => "target.role.name",
                    _ => null
                };

                if (field is not null && property.GetValue(_request) is { } value)
                    logEvent.AddPropertyIfAbsent(factory.CreateProperty(field, value));
            }
        }
    }
}
