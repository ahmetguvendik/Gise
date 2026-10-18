using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Gise.HealthChecks;

public static class HealthChecksBuilderExtensions
{
    // Worker, WorkerHeartbeat'i alıp her turun sonunda Beat() çağırır. Son tur staleAfter'dan eskiyse
    // "ready" kontrolü Unhealthy döner.
    public static IHealthChecksBuilder AddWorkerHeartbeat(this IHealthChecksBuilder builder, TimeSpan staleAfter)
    {
        builder.Services.TryAddSingleton<WorkerHeartbeat>();

        return builder.Add(new HealthCheckRegistration(
            "worker-heartbeat",
            services => new WorkerHeartbeatHealthCheck(services.GetRequiredService<WorkerHeartbeat>(), staleAfter),
            failureStatus: null,
            tags: ["ready"]));
    }
}
