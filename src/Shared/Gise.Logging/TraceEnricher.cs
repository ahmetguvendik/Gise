using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace Gise.Logging;

// Aynı isteğin farklı servislerdeki logları ve APM'deki izi bu kimliklerle birbirine bağlanır.
public sealed class TraceEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var activity = Activity.Current;
        if (activity is null)
            return;

        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("trace.id", activity.TraceId.ToString()));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("span.id", activity.SpanId.ToString()));
    }
}
