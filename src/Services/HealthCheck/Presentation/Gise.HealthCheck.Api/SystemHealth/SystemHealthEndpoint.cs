using Gise.HealthCheck.Api.Authentication;
using HealthChecks.UI.Data;
using Microsoft.EntityFrameworkCore;

namespace Gise.HealthCheck.Api.SystemHealth;

// Admin sayfasının (Yönetim → Sistem durumu) verisi: panelin son yoklama sonuçları, gruplanmış. Panelin kendi arayüzü
// yalnızca 127.0.0.1'den açık; gateway'den bu uç yayınlanır ve yalnızca admin okur.
public static class SystemHealthEndpoint
{
    private const string Unknown = "Unknown";

    public static IEndpointRouteBuilder MapSystemHealth(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/system/health", async (HealthChecksDb db, TimeProvider clock, CancellationToken cancellationToken) =>
        {
            var configurations = await db.Configurations.AsNoTracking().ToListAsync(cancellationToken);
            var executions = await db.Executions.AsNoTracking().Include(e => e.Entries).ToListAsync(cancellationToken);

            var items = configurations
                .Select(configuration =>
                {
                    var execution = executions.FirstOrDefault(e => e.Name == configuration.Name);
                    return new
                    {
                        name = configuration.Name,
                        group = GroupOf(configuration.Name),
                        // Henüz yoklanmadıysa (panel yeni açıldı) "Unknown": yeşil sayılmaz.
                        status = execution?.Status.ToString() ?? Unknown,
                        lastExecuted = execution?.LastExecuted,
                        onStateFrom = execution?.OnStateFrom,
                        checks = (execution?.Entries ?? [])
                            .OrderBy(entry => entry.Name, StringComparer.Ordinal)
                            .Select(entry => new
                            {
                                name = entry.Name,
                                status = entry.Status.ToString(),
                                description = entry.Description,
                                durationMs = (int)entry.Duration.TotalMilliseconds
                            })
                            .ToList()
                    };
                })
                .OrderBy(item => item.name, StringComparer.Ordinal)
                .ToList();

            return Results.Json(new
            {
                status = Overall(items.Select(i => i.status)),
                checkedAt = clock.GetUtcNow().UtcDateTime,
                groups = items
                    .GroupBy(item => item.group)
                    .OrderBy(group => GroupOrder(group.Key))
                    .Select(group => new { name = group.Key, items = group.ToList() })
                    .ToList()
            });
        }).RequireAuthorization(Policies.Admin);

        return endpoints;
    }

    // Ad kuralı: "...-api" API, "...-worker" worker, kalanı (altyapı, hata oranı) altyapı ve izleme.
    internal static string GroupOf(string name) => name.EndsWith("-api", StringComparison.Ordinal) ? "API'ler"
        : name.EndsWith("-worker", StringComparison.Ordinal) ? "Worker'lar"
        : "Altyapı ve izleme";

    private static int GroupOrder(string group) => group switch { "API'ler" => 0, "Worker'lar" => 1, _ => 2 };

    // Bir tane sağlıksız varsa sağlıksız; yoksa bir tane sarı varsa sarı; yoklanmamış varsa bilinmiyor.
    internal static string Overall(IEnumerable<string> statuses)
    {
        var all = statuses.ToList();
        return all.Contains("Unhealthy") ? "Unhealthy"
            : all.Contains("Degraded") ? "Degraded"
            : all.Contains(Unknown) || all.Count == 0 ? Unknown
            : "Healthy";
    }
}
