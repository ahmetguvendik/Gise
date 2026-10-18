namespace Gise.HealthChecks;

// Worker döngüsünün her turda attığı nabız. Çöken process'i restart toparlar, ama veritabanı
// ya da Kafka çağrısında asılı kalan bir döngü sessizce durur; bunu yalnızca nabız yakalar.
public sealed class WorkerHeartbeat
{
    private long _lastBeatTicks = DateTime.UtcNow.Ticks;

    public void Beat() => Interlocked.Exchange(ref _lastBeatTicks, DateTime.UtcNow.Ticks);

    public DateTime LastBeatUtc => new(Interlocked.Read(ref _lastBeatTicks), DateTimeKind.Utc);
}
