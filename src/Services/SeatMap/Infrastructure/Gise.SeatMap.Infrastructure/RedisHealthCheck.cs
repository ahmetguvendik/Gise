using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace Gise.SeatMap.Infrastructure;

// Koltuk değişiklikleri Redis'ten gelir; ulaşılamıyorsa harita anlık güncellenmez.
public sealed class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _redis;

    public RedisHealthCheck(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await _redis.GetDatabase().PingAsync();
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex) when (ex is RedisException or TimeoutException)
        {
            return HealthCheckResult.Unhealthy("Redis'e ulaşılamıyor.", ex);
        }
    }
}
