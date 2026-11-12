using Microsoft.EntityFrameworkCore;

namespace Gise.AuditLog.Worker;

// Saklama süresi dolan denetim kayıtlarını siler (kişisel veri: kullanıcı kimliği ve IP; KVKK süresiz saklamaya izin vermez).
// Tablodaki tek silme budur. Ayarlar appsettings'teki Retention bölümünden.
public sealed class RetentionService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RetentionService> _logger;
    private readonly TimeSpan _retention;
    private readonly TimeSpan _interval;

    public RetentionService(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<RetentionService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        var retention = configuration.GetRequiredSection("Retention");
        _retention = TimeSpan.FromDays(retention.GetValue<int>("Days"));
        _interval = TimeSpan.FromHours(retention.GetValue<int>("CleanupIntervalHours"));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Açılışı tutmasın.
        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var cutoff = DateTime.UtcNow - _retention;

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
                var deleted = await db.AuditRecords.Where(r => r.OccurredAt < cutoff).ExecuteDeleteAsync(stoppingToken);

                if (deleted > 0)
                    _logger.LogInformation("{Count} denetim kaydı saklama süresi ({Days} gün) dolduğu için silindi.", deleted, _retention.TotalDays);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Süresi dolan denetim kayıtları silinemedi; bir sonraki turda tekrar denenecek.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}
