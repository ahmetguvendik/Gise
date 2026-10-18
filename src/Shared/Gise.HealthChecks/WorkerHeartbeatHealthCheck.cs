using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Gise.HealthChecks;

internal sealed class WorkerHeartbeatHealthCheck : IHealthCheck
{
    private readonly WorkerHeartbeat _heartbeat;
    private readonly TimeSpan _staleAfter;

    public WorkerHeartbeatHealthCheck(WorkerHeartbeat heartbeat, TimeSpan staleAfter)
    {
        _heartbeat = heartbeat;
        _staleAfter = staleAfter;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var age = DateTime.UtcNow - _heartbeat.LastBeatUtc;

        var result = age > _staleAfter
            ? HealthCheckResult.Unhealthy($"Worker döngüsü {age.TotalSeconds:F0} sn'dir tur atmadı (eşik {_staleAfter.TotalSeconds:F0} sn).")
            : HealthCheckResult.Healthy($"Son tur {age.TotalSeconds:F0} sn önce.");

        return Task.FromResult(result);
    }
}
