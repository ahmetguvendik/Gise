using System.Text.Json;
using Gise.Contracts.Events;
using Gise.Contracts.Grpc.Catalog;
using Gise.HealthChecks;
using Gise.Search.Application.Interfaces;
using Gise.Search.Domain.Documents;
using Gise.Search.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gise.Search.InboxWorker;

// Arama dizinini (Elasticsearch) güncel tutar (bkz. tasarım 3.10). Açılışta dizin yoksa kurar, boşsa yayındaki
// etkinlikleri Catalog'dan gRPC ile sayfa sayfa alıp doldurur: Kafka eski olayları sildiği için topic'i baştan okumak
// yetmez. Sonra Kafka'dan gelip inbox'a yazılan olayları işler. Ayarlar appsettings'teki Inbox ve Catalog bölümlerinden.
public sealed class Worker : BackgroundService
{
    // gRPC'de bir sayfadaki etkinlik sayısı; Catalog en fazla 500 kabul ediyor.
    private const int ReindexPageSize = 200;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<Worker> _logger;
    private readonly WorkerHeartbeat _heartbeat;
    private readonly IEventIndex _eventIndex;
    private readonly CatalogGrpc.CatalogGrpcClient _catalog;
    private readonly TimeSpan _grpcTimeout;
    private readonly int _batchSize;
    private readonly TimeSpan _pollInterval;
    private readonly int _maxRetryCount;
    private readonly TimeSpan _retention;
    private readonly TimeSpan _cleanupInterval;

    private DateTime _lastCleanupAt = DateTime.MinValue;
    private bool _indexReady;

    public Worker(
        IServiceScopeFactory scopeFactory,
        ILogger<Worker> logger,
        IConfiguration configuration,
        WorkerHeartbeat heartbeat,
        IEventIndex eventIndex,
        CatalogGrpc.CatalogGrpcClient catalog)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _heartbeat = heartbeat;
        _eventIndex = eventIndex;
        _catalog = catalog;
        _grpcTimeout = TimeSpan.FromSeconds(configuration.GetRequiredSection("Catalog").GetValue<int>("GrpcTimeoutSeconds"));

        var inbox = configuration.GetRequiredSection("Inbox");
        _batchSize = inbox.GetValue<int>("BatchSize");
        _pollInterval = TimeSpan.FromSeconds(inbox.GetValue<int>("PollIntervalSeconds"));
        _maxRetryCount = inbox.GetValue<int>("MaxRetryCount");
        _retention = TimeSpan.FromDays(inbox.GetValue<int>("RetentionDays"));
        _cleanupInterval = TimeSpan.FromMinutes(inbox.GetValue<int>("CleanupIntervalMinutes"));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Dizin hazır değilken olay işlenmez; olaylar inbox'ta bekler.
                if (!_indexReady)
                {
                    await PrepareIndexAsync(stoppingToken);
                    _indexReady = true;
                }

                await ProcessPendingAsync(stoppingToken);
                await CleanupAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                // Tur başarısız oldu (veritabanı, Elasticsearch ya da Catalog yok); bir sonraki turda tekrar denenir.
                _logger.LogError(ex, "Inbox turu tamamlanamadı.");
            }

            // try/catch'in dışında: hata alıp dönmeye devam eden döngü canlıdır; asılı kalan döngü
            // bu satıra hiç ulaşamaz ve sağlık kontrolü bunu yakalar.
            _heartbeat.Beat();

            await Task.Delay(_pollInterval, stoppingToken);
        }
    }

    // Her mesaj kendi transaction'ında işlenir: biri başarısız olursa yalnızca onun yarım kalan değişikliği geri alınır.
    private async Task ProcessPendingAsync(CancellationToken cancellationToken)
    {
        var processed = 0;

        for (var i = 0; i < _batchSize; i++)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<InboxWorkerDbContext>();

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

            // SKIP LOCKED: worker'ın birden çok kopyası aynı mesajı birlikte işlemez.
            // NOT EXISTS: aynı etkinliğin daha eski bir olayı beklerken yenisi onu geçmez.
            var message = (await db.InboxMessages
                .FromSql($"""
                    SELECT m.* FROM inbox_messages m
                    WHERE m.processed_at IS NULL
                      AND NOT m.is_dead_lettered
                      AND (m.next_attempt_at IS NULL OR m.next_attempt_at <= now())
                      AND NOT EXISTS (
                          SELECT 1 FROM inbox_messages earlier
                          WHERE earlier.key = m.key
                            AND earlier.processed_at IS NULL
                            AND NOT earlier.is_dead_lettered
                            AND earlier.created_at < m.created_at)
                    ORDER BY m.created_at
                    LIMIT 1
                    FOR UPDATE OF m SKIP LOCKED
                    """)
                .ToListAsync(cancellationToken))
                .FirstOrDefault();

            if (message is null)
                break;

            try
            {
                await HandleAsync(message, cancellationToken);

                message.ProcessedAt = DateTime.UtcNow;
                message.Error = null;
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                processed++;
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                // Deneme bilgisi kaydedilir; dizine yazma tekrar edilse de sonuç aynı.
                await transaction.RollbackAsync(cancellationToken);
                await RecordFailureAsync(db, message, ex, cancellationToken);
            }
        }

        if (processed > 0)
            _logger.LogInformation("Inbox: {Count} olay işlendi.", processed);
    }

    private async Task PrepareIndexAsync(CancellationToken cancellationToken)
    {
        await _eventIndex.EnsureCreatedAsync(cancellationToken);
        if (await _eventIndex.CountAsync(cancellationToken) > 0)
            return;

        // Boş dizin: ilk açılış ya da dizin silindi (yeniden dizinleme böyle tetiklenir). Sonra işlenecek olaylar
        // bu kopyanın üzerine sırayla uygulanır; aynı etkinliğin en son olayı en son işlendiği için sonuç güncel kalır.
        var total = 0;
        for (var page = 0; ; page++)
        {
            var reply = await _catalog.ListPublishedEventsAsync(
                new ListPublishedEventsRequest { Page = page, PageSize = ReindexPageSize },
                deadline: DateTime.UtcNow.Add(_grpcTimeout),
                cancellationToken: cancellationToken);

            await _eventIndex.UpsertManyAsync(reply.Events.Select(ToDocument).ToList(), cancellationToken);
            total += reply.Events.Count;

            if (!reply.HasMore)
                break;
        }

        _logger.LogInformation("Arama dizini Catalog'dan kuruldu: {Count} etkinlik.", total);
    }

    private async Task HandleAsync(InboxMessage message, CancellationToken cancellationToken)
    {
        switch (message.Type)
        {
            case nameof(EventPublished):
                var published = Deserialize<EventPublished>(message);
                await _eventIndex.UpsertAsync(ToDocument(published.EventId, published.VenueId, published.Title, published.StartsAt,
                    published.VenueName, published.City, published.MinPrice, published.PosterKey), cancellationToken);
                break;

            // Ad, tarih, fiyat ya da afiş değişti; kayıt olayın taşıdığı güncel hâlle değişir.
            case nameof(EventUpdated):
                var updated = Deserialize<EventUpdated>(message);
                await _eventIndex.UpsertAsync(ToDocument(updated.EventId, updated.VenueId, updated.Title, updated.StartsAt,
                    updated.VenueName, updated.City, updated.MinPrice, updated.PosterKey), cancellationToken);
                break;

            // İptal edilen etkinlik aramada çıkmaz.
            case nameof(EventCancelled):
                await _eventIndex.DeleteAsync(Deserialize<EventCancelled>(message).EventId, cancellationToken);
                break;

            default:
                break;
        }
    }

    private static EventDocument ToDocument(PublishedEventItem item) => ToDocument(
        Guid.Parse(item.EventId), Guid.Parse(item.VenueId), item.Title, item.StartsAt.ToDateTime(),
        item.VenueName, item.City, item.MinPriceKurus / 100m, string.IsNullOrEmpty(item.PosterKey) ? null : item.PosterKey);

    private static EventDocument ToDocument(
        Guid eventId, Guid venueId, string title, DateTime startsAt, string venueName, string city, decimal minPrice, string? posterKey) => new()
    {
        Id = eventId,
        VenueId = venueId,
        Title = title,
        StartsAt = DateTime.SpecifyKind(startsAt, DateTimeKind.Utc),
        VenueName = venueName,
        City = city,
        MinPrice = minPrice,
        PosterKey = posterKey
    };

    private static T Deserialize<T>(InboxMessage message)
        => JsonSerializer.Deserialize<T>(message.Payload, JsonSerializerOptions.Web)
            ?? throw new InvalidOperationException($"{message.Type} gövdesi okunamadı.");

    // Artan bekleme: 2, 4, 8 … saniye, en fazla 5 dakika; sınır aşılınca dead-letter.
    private async Task RecordFailureAsync(InboxWorkerDbContext db, InboxMessage message, Exception exception, CancellationToken cancellationToken)
    {
        db.ChangeTracker.Clear();

        var retryCount = message.RetryCount + 1;
        var deadLettered = retryCount >= _maxRetryCount;
        var error = exception.Message.Length > 2000 ? exception.Message[..2000] : exception.Message;
        DateTime? nextAttemptAt = deadLettered ? null : DateTime.UtcNow.AddSeconds(Math.Min(Math.Pow(2, retryCount), 300));

        await db.InboxMessages
            .Where(m => m.Id == message.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(m => m.RetryCount, retryCount)
                .SetProperty(m => m.Error, error)
                .SetProperty(m => m.NextAttemptAt, nextAttemptAt)
                .SetProperty(m => m.IsDeadLettered, deadLettered)
                .SetProperty(m => m.UpdatedAt, DateTime.UtcNow), cancellationToken);

        if (deadLettered)
            _logger.LogError(exception, "Inbox mesajı {Id} ({Type}) {RetryCount} denemede işlenemedi, dead-letter olarak işaretlendi.",
                message.Id, message.Type, retryCount);
        else
            _logger.LogWarning(exception, "Inbox mesajı {Id} ({Type}) işlenemedi, {RetryCount}. deneme {NextAttemptAt} sonrasında.",
                message.Id, message.Type, retryCount, nextAttemptAt);
    }

    // İşlenmiş satırlar bir süre sonra silinir; tablo sonsuza kadar büyümez.
    private async Task CleanupAsync(CancellationToken cancellationToken)
    {
        if (DateTime.UtcNow - _lastCleanupAt < _cleanupInterval)
            return;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InboxWorkerDbContext>();

        var threshold = DateTime.UtcNow - _retention;
        var deleted = await db.InboxMessages
            .Where(m => m.ProcessedAt != null && m.ProcessedAt < threshold)
            .ExecuteDeleteAsync(cancellationToken);

        _lastCleanupAt = DateTime.UtcNow;

        if (deleted > 0)
            _logger.LogInformation("Inbox: {Count} eski satır temizlendi.", deleted);
    }
}
