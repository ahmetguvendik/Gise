using System.Text.Json;
using Gise.Contracts.Events;
using Gise.Contracts.Grpc.Catalog;
using Gise.Exceptions;
using Gise.HealthChecks;
using Gise.Reservation.Domain.Entities;
using Gise.Reservation.Domain.Enums;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace Gise.Reservation.InboxWorker;

// Kafka'dan gelip inbox'a yazılan Catalog olaylarını işler (bkz. tasarım 2.2): yayınlanan etkinliğin koltuk stoğunu
// Catalog'dan gRPC ile kurar, tarih ve fiyat değişikliğini yansıtır, iptal edilen etkinliğin satışını kapatır.
// Ayarlar appsettings'teki Inbox ve Catalog bölümlerinden okunur.
public sealed class Worker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<Worker> _logger;
    private readonly WorkerHeartbeat _heartbeat;
    private readonly int _batchSize;
    private readonly TimeSpan _pollInterval;
    private readonly int _maxRetryCount;
    private readonly TimeSpan _retention;
    private readonly TimeSpan _cleanupInterval;
    private readonly TimeSpan _grpcTimeout;

    private DateTime _lastCleanupAt = DateTime.MinValue;

    public Worker(IServiceScopeFactory scopeFactory, ILogger<Worker> logger, IConfiguration configuration, WorkerHeartbeat heartbeat)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _heartbeat = heartbeat;

        var inbox = configuration.GetRequiredSection("Inbox");
        _batchSize = inbox.GetValue<int>("BatchSize");
        _pollInterval = TimeSpan.FromSeconds(inbox.GetValue<int>("PollIntervalSeconds"));
        _maxRetryCount = inbox.GetValue<int>("MaxRetryCount");
        _retention = TimeSpan.FromDays(inbox.GetValue<int>("RetentionDays"));
        _cleanupInterval = TimeSpan.FromMinutes(inbox.GetValue<int>("CleanupIntervalMinutes"));

        _grpcTimeout = TimeSpan.FromSeconds(configuration.GetRequiredSection("Catalog").GetValue<int>("GrpcTimeoutSeconds"));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingAsync(stoppingToken);
                await CleanupAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                // Tur başarısız oldu (veritabanı yok); bir sonraki turda tekrar denenir.
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
            var catalog = scope.ServiceProvider.GetRequiredService<CatalogGrpc.CatalogGrpcClient>();

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

            // SKIP LOCKED: worker'ın birden çok kopyası aynı mesajı birlikte işlemez.
            // NOT EXISTS: aynı etkinliğin daha eski bir olayı beklerken yenisi onu geçmez; yoksa "iptal edildi",
            // stok "yayınlandı" ile kurulmadan işlenebilirdi.
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
                await HandleAsync(db, catalog, message, cancellationToken);

                message.ProcessedAt = DateTime.UtcNow;
                message.Error = null;
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                processed++;
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                // Yarım kalan stok değişikliği geri alınır; yalnızca deneme bilgisi kaydedilir.
                await transaction.RollbackAsync(cancellationToken);
                await RecordFailureAsync(db, message, ex, cancellationToken);
            }
        }

        if (processed > 0)
            _logger.LogInformation("Inbox: {Count} olay işlendi.", processed);
    }

    private async Task HandleAsync(InboxWorkerDbContext db, CatalogGrpc.CatalogGrpcClient catalog, InboxMessage message, CancellationToken cancellationToken)
    {
        switch (message.Type)
        {
            case nameof(EventPublished):
                await OpenStockAsync(db, catalog, Deserialize<EventPublished>(message), cancellationToken);
                break;

            case nameof(EventUpdated):
                await UpdateStockAsync(db, catalog, Deserialize<EventUpdated>(message), cancellationToken);
                break;

            case nameof(EventCancelled):
                await CloseStockAsync(db, Deserialize<EventCancelled>(message), cancellationToken);
                break;

            default:
                // Catalog'un Reservation'ı ilgilendirmeyen bir olayı; işlenmiş sayılır.
                _logger.LogInformation("İlgilenilmeyen olay tipi atlandı: {Type}", message.Type);
                break;
        }
    }

    // Yayınlanan etkinliğin koltukları Catalog'dan alınıp stok kurulur. Olay ikinci kez gelirse stok zaten vardır.
    private async Task OpenStockAsync(InboxWorkerDbContext db, CatalogGrpc.CatalogGrpcClient catalog, EventPublished evt, CancellationToken cancellationToken)
    {
        if (await db.EventStocks.AnyAsync(s => s.Id == evt.EventId, cancellationToken))
            return;

        var seats = await GetSeatsAsync(catalog, evt.EventId, cancellationToken);
        if (seats is null)
        {
            // Yayınlanıp hemen iptal edilmiş: satışa hiç açılmaz. Ardından gelen EventCancelled zararsızdır.
            db.EventStocks.Add(new EventStock { Id = evt.EventId, StartsAt = evt.StartsAt, CancelledAt = DateTime.UtcNow });
            return;
        }

        db.EventStocks.Add(new EventStock
        {
            Id = evt.EventId,
            StartsAt = evt.StartsAt,
            Seats = seats.Select(seat => new EventSeat
            {
                EventId = evt.EventId,
                SeatId = Guid.Parse(seat.SeatId),
                SectionId = Guid.Parse(seat.SectionId),
                SectionName = seat.SectionName,
                Row = seat.Row,
                Number = seat.Number,
                Price = seat.PriceKurus / 100m
            }).ToList()
        });

        _logger.LogInformation("Stok kuruluyor: {EventId}, {Count} koltuk.", evt.EventId, seats.Count);
    }

    // Tarih ve fiyat değişikliği. Olay yalnızca en düşük fiyatı taşıdığı için bölüm fiyatları Catalog'dan yeniden okunur.
    private async Task UpdateStockAsync(InboxWorkerDbContext db, CatalogGrpc.CatalogGrpcClient catalog, EventUpdated evt, CancellationToken cancellationToken)
    {
        // Aynı etkinliğin olayları sırayla işlendiği için stok EventPublished ile kurulmuş olmalı.
        var stock = await db.EventStocks.Include(s => s.Seats).SingleOrDefaultAsync(s => s.Id == evt.EventId, cancellationToken)
            ?? throw new InvalidOperationException($"'{evt.EventId}' için stok yok; önce EventPublished işlenmeli.");

        if (stock.CancelledAt is not null)
            return;

        stock.StartsAt = evt.StartsAt;

        var seats = await GetSeatsAsync(catalog, evt.EventId, cancellationToken);
        if (seats is null)
            return; // Bu arada iptal edilmiş; EventCancelled ardından gelir.

        var prices = seats.ToDictionary(s => Guid.Parse(s.SeatId), s => s.PriceKurus / 100m);
        foreach (var seat in stock.Seats)
        {
            if (prices.TryGetValue(seat.SeatId, out var price))
                seat.Price = price;
        }
    }

    // Satış durur, açık tutmalar bırakılmış sayılır. Redis'teki kilitler süre dolunca kendiliğinden kalkar; o arada
    // yeni tutma zaten açılamaz, çünkü stok iptal. Satılmış koltukların iadesi Order'ın işi (bkz. tasarım 4.4).
    private async Task CloseStockAsync(InboxWorkerDbContext db, EventCancelled evt, CancellationToken cancellationToken)
    {
        var stock = await db.EventStocks.SingleOrDefaultAsync(s => s.Id == evt.EventId, cancellationToken);
        if (stock is null)
        {
            _logger.LogWarning("İptal edilen etkinliğin stoğu yok, satışa hiç açılmamış: {EventId}", evt.EventId);
            return;
        }

        if (stock.CancelledAt is not null)
            return;

        stock.CancelledAt = evt.CancelledAt;

        var holds = await db.SeatHolds
            .Where(h => h.EventId == evt.EventId && h.Status == HoldStatus.Active)
            .ToListAsync(cancellationToken);
        foreach (var hold in holds)
            hold.Status = HoldStatus.Released;
    }

    // Koltuklar ve güncel fiyatlar. Etkinlik artık yayında değilse (iptal edilmiş) null döner; diğer hatalar
    // mesajı başarısız sayar ve yeniden denenir.
    private async Task<IReadOnlyList<SeatItem>?> GetSeatsAsync(CatalogGrpc.CatalogGrpcClient catalog, Guid eventId, CancellationToken cancellationToken)
    {
        try
        {
            var reply = await catalog.GetEventSeatsAsync(
                new GetEventSeatsRequest { EventId = eventId.ToString() },
                deadline: DateTime.UtcNow.Add(_grpcTimeout),
                cancellationToken: cancellationToken);

            return reply.Seats;
        }
        catch (RpcException ex) when (ex.Trailers.GetValue("error-code") is ErrorCodes.Event.NotPublished or ErrorCodes.Event.Cancelled)
        {
            return null;
        }
    }

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
