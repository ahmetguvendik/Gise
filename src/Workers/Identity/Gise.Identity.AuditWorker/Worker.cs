using Gise.HealthChecks;
using Gise.Identity.Application.Auditing;
using Gise.Logging;

namespace Gise.Identity.AuditWorker;

// Keycloak olaylarını denetim loguna aktaran döngü (Telgraf'taki KeycloakEventPollerService; iş KeycloakEventSync'te).
// Ayarlar appsettings'teki EventSync bölümünden.
public sealed class Worker : BackgroundService
{
    private readonly KeycloakEventSync _sync;
    private readonly WorkerHeartbeat _heartbeat;
    private readonly ILogger<Worker> _logger;
    private readonly TimeSpan _interval;
    private readonly TimeSpan _retryDelay;

    public Worker(KeycloakEventSync sync, WorkerHeartbeat heartbeat, ILogger<Worker> logger, IConfiguration configuration)
    {
        _sync = sync;
        _heartbeat = heartbeat;
        _logger = logger;

        var settings = configuration.GetRequiredSection("EventSync");
        _interval = TimeSpan.FromSeconds(settings.GetValue<int>("IntervalSeconds"));
        _retryDelay = TimeSpan.FromSeconds(settings.GetValue<int>("RetryDelaySeconds"));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = _interval;
            try
            {
                // Her tur APM'de tek işlem: Keycloak çağrıları onun altında görünür.
                using var activity = AppTracing.StartLinked("identity.audit-sync", traceParent: null);
                await _sync.RunAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                // Keycloak yok: imleç ilerlemedi, olaylar bir sonraki turda okunur.
                _logger.LogError(ex, "Keycloak olayları okunamadı.");
                delay = _retryDelay;
            }

            // try/catch'in dışında: hata alıp dönmeye devam eden döngü canlıdır; asılı kalan döngü
            // bu satıra hiç ulaşamaz ve sağlık kontrolü bunu yakalar.
            _heartbeat.Beat();

            await Task.Delay(delay, stoppingToken);
        }
    }
}
