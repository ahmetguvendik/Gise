using System.Runtime.CompilerServices;
using Gise.Contracts.Grpc.Catalog;
using Gise.HealthChecks;
using Gise.Search.Application.Interfaces;
using Gise.Search.Domain.Documents;

namespace Gise.Search.SyncWorker;

// Arama dizinini Catalog'dan düzenli olarak baştan kurar (bkz. tasarım 3.10): yayındaki etkinlikler gRPC ile sayfa sayfa
// alınır, yeni dizine yazılır, arama tek adımda yeni dizine geçer. İptal edilen ya da başlamış etkinlik bir sonraki turda
// düşer. Kafka dinlemez. Ayarlar appsettings'teki Sync ve Catalog bölümlerinden.
public sealed class Worker : BackgroundService
{
    // gRPC'de bir sayfadaki etkinlik sayısı; Catalog en fazla 500 kabul ediyor.
    private const int PageSize = 200;

    private readonly IEventIndex _eventIndex;
    private readonly CatalogGrpc.CatalogGrpcClient _catalog;
    private readonly WorkerHeartbeat _heartbeat;
    private readonly ILogger<Worker> _logger;
    private readonly TimeSpan _interval;
    private readonly TimeSpan _retryDelay;
    private readonly TimeSpan _grpcTimeout;

    public Worker(
        IEventIndex eventIndex,
        CatalogGrpc.CatalogGrpcClient catalog,
        WorkerHeartbeat heartbeat,
        ILogger<Worker> logger,
        IConfiguration configuration)
    {
        _eventIndex = eventIndex;
        _catalog = catalog;
        _heartbeat = heartbeat;
        _logger = logger;

        var sync = configuration.GetRequiredSection("Sync");
        _interval = TimeSpan.FromSeconds(sync.GetValue<int>("IntervalSeconds"));
        _retryDelay = TimeSpan.FromSeconds(sync.GetValue<int>("RetryDelaySeconds"));
        _grpcTimeout = TimeSpan.FromSeconds(configuration.GetRequiredSection("Catalog").GetValue<int>("GrpcTimeoutSeconds"));
    }

    // İlk tur hemen: dizin yoksa arama açılışta boş kalmasın.
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = _interval;
            try
            {
                var count = await _eventIndex.ReplaceAllAsync(FetchPublishedEventsAsync(stoppingToken), stoppingToken);
                _logger.LogInformation("Arama dizini yenilendi: {Count} etkinlik.", count);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                // Catalog ya da Elasticsearch yok: arama eski dizinle devam eder, kısa süre sonra tekrar denenir.
                _logger.LogError(ex, "Arama dizini yenilenemedi.");
                delay = _retryDelay;
            }

            // try/catch'in dışında: hata alıp dönmeye devam eden döngü canlıdır; asılı kalan döngü
            // bu satıra hiç ulaşamaz ve sağlık kontrolü bunu yakalar.
            _heartbeat.Beat();

            await Task.Delay(delay, stoppingToken);
        }
    }

    // Sayfalar sırayla istenir. Tur sürerken yayınlanan bir etkinlik sayfaları kaydırıp atlanırsa bir sonraki turda gelir.
    private async IAsyncEnumerable<EventDocument> FetchPublishedEventsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (var page = 0; ; page++)
        {
            var reply = await _catalog.ListPublishedEventsAsync(
                new ListPublishedEventsRequest { Page = page, PageSize = PageSize },
                deadline: DateTime.UtcNow.Add(_grpcTimeout),
                cancellationToken: cancellationToken);

            foreach (var item in reply.Events)
                yield return ToDocument(item);

            if (!reply.HasMore)
                yield break;
        }
    }

    private static EventDocument ToDocument(PublishedEventItem item) => new()
    {
        Id = Guid.Parse(item.EventId),
        VenueId = Guid.Parse(item.VenueId),
        Title = item.Title,
        StartsAt = item.StartsAt.ToDateTime(),
        VenueName = item.VenueName,
        City = item.City,
        MinPrice = item.MinPriceKurus / 100m,
        PosterKey = string.IsNullOrEmpty(item.PosterKey) ? null : item.PosterKey
    };
}
