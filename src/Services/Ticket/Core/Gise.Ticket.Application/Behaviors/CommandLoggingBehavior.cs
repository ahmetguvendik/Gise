using System.Diagnostics;
using System.Text.RegularExpressions;
using Gise.Ticket.Application.Interfaces;
using Gise.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using Serilog.Core;
using Serilog.Events;

namespace Gise.Ticket.Application.Behaviors;

// "Kim ne yaptı" kaydı (bkz. tasarım 8; Telgraf'taki CommandLoggingBehavior). Her command bitince tek satır: kim (actor.id
// ve actor.roles istekten gelir; worker'daki sistem adımlarında boştur, Logstash "system" yazar), ne (event.action), hangi
// kayıt (komut ve yanıttaki kimlikler, target.*), ne değişti (ChangeAuditInterceptor) ve sonuç. Telgraf'tan farkı:
// başarısız denemeler de yazılır (event.outcome failure, error.code). event.dataset "gise.audit": Logstash bu satırları
// 1 yıl saklanan ayrı akışa yazar. Query'ler yazılmaz. ValidationBehavior'dan önce kayıtlı: doğrulamaya takılan deneme de
// görünür.
public sealed partial class CommandLoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<CommandLoggingBehavior<TRequest, TResponse>> _logger;
    private readonly IChangeAuditCollector _changes;

    public CommandLoggingBehavior(
        ILogger<CommandLoggingBehavior<TRequest, TResponse>> logger,
        IChangeAuditCollector changes)
    {
        _logger = logger;
        _changes = changes;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var name = typeof(TRequest).Name;

        if (!name.EndsWith("Command", StringComparison.Ordinal))
            return await next(cancellationToken);

        var action = name[..^"Command".Length];
        var stopwatch = Stopwatch.StartNew();
        TResponse response;

        try
        {
            response = await next(cancellationToken);
        }
        catch (Exception ex)
        {
            // Kaydedilemeyen değişiklikler aynı kapsamdaki bir sonraki command'a karışmasın.
            _changes.Drain();

            var (errorCode, statusCode) = ex is AppException appException
                ? (appException.ErrorCode, appException.StatusCode)
                : (ErrorCodes.General.InternalError, 500);

            // StatusCode: istemcinin aldığı yanıt (Logstash http.response.status_code yapar; worker'da kaldırır).
            using (LogContext.Push(new AuditFieldsEnricher(action, request, response: null, errorCode)))
            {
                _logger.LogWarning("{Command:l} başarısız: {ErrorCode:l} ({StatusCode}, {ElapsedMs} ms).",
                    action, errorCode, statusCode, stopwatch.ElapsedMilliseconds);
            }

            throw;
        }

        var changes = Summarize(_changes.Drain());

        using (LogContext.Push(new AuditFieldsEnricher(action, request, response, errorCode: null)))
        {
            if (changes.Length == 0)
            {
                _logger.LogInformation("{Command:l} tamamlandı ({ElapsedMs} ms).",
                    action, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogInformation("{Command:l} tamamlandı ({ElapsedMs} ms). Değişenler: {Changes:l}",
                    action, stopwatch.ElapsedMilliseconds, changes);
            }
        }

        return response;
    }

    // Aynı değişiklik birden çok satırda olabilir (bir siparişin koltukları): bir kez, adediyle yazılır.
    private static string Summarize(IReadOnlyList<string> changes) => string.Join(" | ", changes
        .GroupBy(change => change)
        .Select(group => group.Count() == 1 ? group.Key : $"{group.Key} (×{group.Count()})"));

    private sealed partial class AuditFieldsEnricher : ILogEventEnricher
    {
        private readonly string _action;
        private readonly string _entity;
        private readonly object?[] _sources;
        private readonly string? _errorCode;

        public AuditFieldsEnricher(string action, object request, object? response, string? errorCode)
        {
            _action = action;
            _entity = EntityOf(action);
            _sources = [request, response];
            _errorCode = errorCode;
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory factory)
        {
            logEvent.AddPropertyIfAbsent(factory.CreateProperty("event.dataset", "gise.audit"));
            logEvent.AddPropertyIfAbsent(factory.CreateProperty("event.action", _action));
            logEvent.AddPropertyIfAbsent(factory.CreateProperty("event.outcome", _errorCode is null ? "success" : "failure"));

            if (_errorCode is not null)
                logEvent.AddPropertyIfAbsent(factory.CreateProperty("error.code", _errorCode));

            foreach (var source in _sources)
            {
                // Bazı command'lar yalnızca yeni kaydın kimliğini döner.
                if (source is Guid id)
                {
                    logEvent.AddPropertyIfAbsent(factory.CreateProperty($"target.{_entity}.id", id));
                    continue;
                }

                if (source is null || source.GetType().IsPrimitive)
                    continue;

                foreach (var property in source.GetType().GetProperties())
                {
                    var fieldName = MapName(property.Name);
                    if (fieldName is null || property.GetIndexParameters().Length > 0)
                        continue;

                    var value = property.GetValue(source);
                    if (value is null)
                        continue;

                    // Command önce geliyor: aynı alan için command'daki değer kazanır.
                    logEvent.AddPropertyIfAbsent(factory.CreateProperty(fieldName, value));
                }
            }
        }

        // Yalnızca kimlikler, etkinlik başlığı ve mekân adı: e-posta, kişi adı, kart ve bilet kodu hiç eşlenmez. "Name"
        // yalnızca mekânda; bilet aktarımındaki "Name" yeni sahibin adı.
        private string? MapName(string propertyName) => propertyName switch
        {
            "Id" => $"target.{_entity}.id",
            "Title" => $"target.{_entity}.title",
            "Name" when _entity == "venue" => "target.venue.name",

            _ when propertyName.EndsWith("Ids", StringComparison.Ordinal) =>
                $"target.{propertyName[..^3].ToLowerInvariant()}.ids",

            _ when propertyName.EndsWith("Id", StringComparison.Ordinal) =>
                $"target.{propertyName[..^2].ToLowerInvariant()}.id",

            _ => null
        };

        // Fiilden sonraki ilk sözcük: "SetEventPoster" → "event", "UpdateVenueLayout" → "venue", "CheckInTicket" → "ticket".
        private static string EntityOf(string action)
        {
            var words = PascalCaseWord().Matches(action).Select(match => match.Value).ToArray();
            var verbLength = words is ["Check", "In", ..] ? 2 : 1;

            return (words.Length > verbLength ? words[verbLength] : action).ToLowerInvariant();
        }

        [GeneratedRegex("[A-Z][a-z0-9]*")]
        private static partial Regex PascalCaseWord();
    }
}
