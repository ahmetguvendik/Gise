using System.Text.Json;
using Gise.Contracts.Events;
using Gise.HealthChecks;
using Gise.Ticket.Domain.Entities;
using Gise.Ticket.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Gise.Ticket.InboxWorker;

// Kafka'dan gelip inbox'a yazılan Catalog olaylarını işler (bkz. tasarım 2.2). Ticket'ı yalnızca iptal ilgilendirir:
// etkinliğin biletleri geçersiz olur. Ayarlar appsettings'teki Inbox bölümünden okunur.
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
                await HandleAsync(db, message, cancellationToken);

                message.ProcessedAt = DateTime.UtcNow;
                message.Error = null;
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                processed++;
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                // Yarım kalan değişiklik geri alınır; yalnızca deneme bilgisi kaydedilir.
                await transaction.RollbackAsync(cancellationToken);
                await RecordFailureAsync(db, message, ex, cancellationToken);
            }
        }

        if (processed > 0)
            _logger.LogInformation("Inbox: {Count} olay işlendi.", processed);
    }

    private async Task HandleAsync(InboxWorkerDbContext db, InboxMessage message, CancellationToken cancellationToken)
    {
        switch (message.Type)
        {
            case nameof(EventCancelled):
                await CancelTicketsAsync(db, Deserialize<EventCancelled>(message), cancellationToken);
                break;

            default:
                // Catalog'un Ticket'ı ilgilendirmeyen bir olayı (yayınlandı, güncellendi); işlenmiş sayılır.
                break;
        }
    }

    // İptal kaydedilir: ödemesi o sırada süren siparişin biletleri sonradan çıkmaz (bkz. IssueTicketsCommandHandler).
    // Geçerli biletler iptal olur; kullanılmışlara dokunulmaz. Paranın iadesi Order'ın işi (bkz. tasarım 4.4).
    private async Task CancelTicketsAsync(InboxWorkerDbContext db, EventCancelled evt, CancellationToken cancellationToken)
    {
        if (await db.CancelledEvents.AnyAsync(e => e.Id == evt.EventId, cancellationToken))
            return;

        db.CancelledEvents.Add(new CancelledEvent { Id = evt.EventId, CancelledAt = evt.CancelledAt });

        var cancelled = await db.Tickets
            .Where(t => t.EventId == evt.EventId && t.Status == TicketStatus.Valid)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(t => t.Status, TicketStatus.Cancelled)
                .SetProperty(t => t.UpdatedAt, DateTime.UtcNow), cancellationToken);

        _logger.LogInformation("Etkinlik iptal edildi: {EventId}, {Count} bilet iptal.", evt.EventId, cancelled);
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
