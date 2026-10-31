using System.Diagnostics;

namespace Gise.Logging;

// Kafka mesajları kendiliğinden izlenmez: outbox gönderimi ve inbox işlemesi, mesajın taşıdığı traceparent'a bağlı bir
// span'le başlatılır. Böylece bir istek API'den Kafka'ya, oradan worker'a kadar APM'de tek iz olarak görünür.
public static class AppTracing
{
    public const string SourceName = "Gise";

    public static readonly ActivitySource Source = new(SourceName);

    public static Activity StartLinked(string name, string? traceParent, ActivityKind kind = ActivityKind.Internal)
    {
        var activity = Source.StartActivity(name, kind, parentId: traceParent!);
        if (activity is not null)
            return activity;

        // Dinleyici yoksa (APM kapalı) null döner; loglardaki trace.id kaybolmasın diye düz Activity.
        var fallback = new Activity(name);
        if (!string.IsNullOrEmpty(traceParent))
            fallback.SetParentId(traceParent);

        return fallback.Start();
    }
}
