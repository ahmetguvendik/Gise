using System.Text.Json;
using Gise.Contracts.Events;
using Gise.HealthChecks;
using Gise.Notification.Application.Emails;
using Gise.Notification.Application.Interfaces;
using Gise.Notification.Domain.Entities;
using Gise.Notification.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Gise.Notification.InboxWorker;

// Kafka'dan gelip inbox'a yazılan olayları işler (bkz. tasarım 2.2, 3.9): biletler çıkınca müşteriye e-posta gönderir ve
// bilet sahibini kaydeder; etkinlik iptal edilince ya da tarihi değişince o etkinliğin bilet sahiplerine yazar.
// Ayarlar appsettings'teki Inbox ve Notification bölümlerinden okunur.
public sealed class Worker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<Worker> _logger;
    private readonly WorkerHeartbeat _heartbeat;
    private readonly IEmailSender _emailSender;
    private readonly int _batchSize;
    private readonly TimeSpan _pollInterval;
    private readonly int _maxRetryCount;
    private readonly TimeSpan _retention;
    private readonly TimeSpan _cleanupInterval;
    private readonly string _ticketsUrl;
    private readonly TimeZoneInfo _timeZone;

    // Aktarım teklifinde alıcının kimliği (sub) kabul edene kadar bilinmez; gönderilen e-posta kaydında bu değer durur.
    // Adres yazılmaz: loglarda alıcı kimliği görünür, adres kişisel veri.
    private const string TransferRecipientId = "transfer-recipient";

    private DateTime _lastCleanupAt = DateTime.MinValue;

    public Worker(IServiceScopeFactory scopeFactory, ILogger<Worker> logger, IConfiguration configuration, WorkerHeartbeat heartbeat, IEmailSender emailSender)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _heartbeat = heartbeat;
        _emailSender = emailSender;

        var inbox = configuration.GetRequiredSection("Inbox");
        _batchSize = inbox.GetValue<int>("BatchSize");
        _pollInterval = TimeSpan.FromSeconds(inbox.GetValue<int>("PollIntervalSeconds"));
        _maxRetryCount = inbox.GetValue<int>("MaxRetryCount");
        _retention = TimeSpan.FromDays(inbox.GetValue<int>("RetentionDays"));
        _cleanupInterval = TimeSpan.FromMinutes(inbox.GetValue<int>("CleanupIntervalMinutes"));

        var notification = configuration.GetRequiredSection("Notification");
        _ticketsUrl = $"{notification["WebBaseUrl"]?.TrimEnd('/')}/biletlerim";
        _timeZone = TimeZoneInfo.FindSystemTimeZoneById(notification["TimeZone"] ?? throw new InvalidOperationException("Notification:TimeZone ayarı eksik."));
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

    // Her mesaj kendi transaction'ında işlenir; satır kilidi (SKIP LOCKED) işlem bitene kadar tutulur.
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
                // Geri alınmaz: gönderilen e-postaların kaydı (sent_notifications) korunur, yeniden denemede aynı kişiye
                // ikinci kez gitmez. Hata veritabanından geldiyse transaction zaten bozuktur; deneme bilgisi de
                // yazılamaz, mesaj sonraki turda yeniden alınır.
                await RecordFailureAsync(db, message, ex, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
        }

        if (processed > 0)
            _logger.LogInformation("Inbox: {Count} olay işlendi.", processed);
    }

    private async Task HandleAsync(InboxWorkerDbContext db, InboxMessage message, CancellationToken cancellationToken)
    {
        switch (message.Type)
        {
            case nameof(TicketsIssued) when message.Topic == Kafka.TicketEventsTopic:
                await OnTicketsIssuedAsync(db, Deserialize<TicketsIssued>(message), cancellationToken);
                break;

            case nameof(TicketTransferOffered) when message.Topic == Kafka.TicketEventsTopic:
                await OnTicketTransferOfferedAsync(db, Deserialize<TicketTransferOffered>(message), cancellationToken);
                break;

            case nameof(TicketTransferred) when message.Topic == Kafka.TicketEventsTopic:
                await OnTicketTransferredAsync(db, Deserialize<TicketTransferred>(message), cancellationToken);
                break;

            case nameof(EventCancelled):
                await OnEventCancelledAsync(db, Deserialize<EventCancelled>(message), cancellationToken);
                break;

            case nameof(EventUpdated):
                await OnEventUpdatedAsync(db, Deserialize<EventUpdated>(message), ReferenceOf(message), cancellationToken);
                break;

            default:
                // Notification'ı ilgilendirmeyen bir olay (ör. EventPublished); işlenmiş sayılır.
                break;
        }
    }

    // Bilet sahibi kaydedilir (iptal ve tarih değişikliği bildirimleri için), müşteriye biletlerin hazır olduğu yazılır.
    private async Task OnTicketsIssuedAsync(InboxWorkerDbContext db, TicketsIssued evt, CancellationToken cancellationToken)
    {
        if (!await db.TicketHolders.AnyAsync(h => h.Id == evt.OrderId, cancellationToken))
        {
            db.TicketHolders.Add(new TicketHolder
            {
                Id = evt.OrderId,
                EventId = evt.EventId,
                CustomerId = evt.CustomerId,
                Email = evt.CustomerEmail,
                Name = evt.CustomerName,
                EventTitle = evt.EventTitle,
                EventStartsAt = evt.EventStartsAt,
                VenueName = evt.VenueName,
                City = evt.City,
                TicketCount = evt.Tickets.Count
            });
            await db.SaveChangesAsync(cancellationToken);
        }

        var email = EmailTemplates.TicketsIssued(
            evt.CustomerName, evt.EventTitle, evt.EventStartsAt, evt.VenueName, evt.City,
            evt.Tickets.Select(t => (t.SectionName, t.Row, t.Number)).ToList(), _ticketsUrl, _timeZone);
        await SendOnceAsync(db, NotificationType.TicketsIssued, evt.OrderId, evt.CustomerId, evt.CustomerEmail, email, cancellationToken);
    }

    // Alıcının hesabı olmayabilir; e-posta yazılan adrese gider.
    private async Task OnTicketTransferOfferedAsync(InboxWorkerDbContext db, TicketTransferOffered evt, CancellationToken cancellationToken)
    {
        var email = EmailTemplates.TicketTransferOffered(
            evt.FromName, evt.EventTitle, evt.EventStartsAt, evt.VenueName, evt.City,
            (evt.SectionName, evt.Row, evt.Number), evt.ExpiresAt, _ticketsUrl, _timeZone);
        await SendOnceAsync(db, NotificationType.TicketTransferOffered, evt.TransferId, TransferRecipientId, evt.ToEmail, email, cancellationToken);
    }

    // Alan kişi bilet sahiplerine eklenir (Id = biletin ID'si), satın alanın bilet sayısı bir azalır: iptal ve erteleme
    // e-postaları bileti o an elinde tutana gider. Gönderene aktarımın tamamlandığı yazılır.
    private async Task OnTicketTransferredAsync(InboxWorkerDbContext db, TicketTransferred evt, CancellationToken cancellationToken)
    {
        // Olay yeniden işlenirse satır zaten vardır; sayı ikinci kez azalmaz.
        if (!await db.TicketHolders.AnyAsync(h => h.Id == evt.TicketId, cancellationToken))
        {
            var buyer = await db.TicketHolders.FirstOrDefaultAsync(h => h.Id == evt.OrderId, cancellationToken);
            if (buyer is { TicketCount: > 0 })
                buyer.TicketCount--;

            db.TicketHolders.Add(new TicketHolder
            {
                Id = evt.TicketId,
                EventId = evt.EventId,
                CustomerId = evt.ToCustomerId,
                Email = evt.ToEmail,
                Name = evt.ToName,
                EventTitle = evt.EventTitle,
                EventStartsAt = evt.EventStartsAt,
                VenueName = evt.VenueName,
                City = evt.City,
                TicketCount = 1
            });
            await db.SaveChangesAsync(cancellationToken);
        }

        var email = EmailTemplates.TicketTransferAccepted(
            evt.FromName, evt.ToEmail, evt.EventTitle, evt.EventStartsAt, evt.VenueName, evt.City,
            (evt.SectionName, evt.Row, evt.Number), _timeZone);
        await SendOnceAsync(db, NotificationType.TicketTransferAccepted, evt.TransferId, evt.FromCustomerId, evt.FromEmail, email, cancellationToken);
    }

    // Biletlerin iptali ve paranın iadesi Ticket ve Order'da yapılır; burada yalnızca bilet sahiplerine yazılır.
    private async Task OnEventCancelledAsync(InboxWorkerDbContext db, EventCancelled evt, CancellationToken cancellationToken)
    {
        foreach (var holder in await RecipientsAsync(db, evt.EventId, cancellationToken))
        {
            var email = EmailTemplates.EventCancelled(holder.Name, holder.EventTitle, holder.EventStartsAt, holder.VenueName, holder.City, evt.Reason, _timeZone);
            await SendOnceAsync(db, NotificationType.EventCancelled, evt.EventId, holder.CustomerId, holder.Email, email, cancellationToken);
        }
    }

    // Etkinliğin güncel hâli bilet sahiplerine kopyalanır; tarih değiştiyse onlara yazılır (fiyat değişikliği bildirilmez).
    private async Task OnEventUpdatedAsync(InboxWorkerDbContext db, EventUpdated evt, Guid referenceId, CancellationToken cancellationToken)
    {
        var holders = await db.TicketHolders.Where(h => h.EventId == evt.EventId).ToListAsync(cancellationToken);
        if (holders.Count == 0)
            return;

        foreach (var holder in holders)
        {
            holder.EventTitle = evt.Title;
            holder.EventStartsAt = evt.StartsAt;
            holder.VenueName = evt.VenueName;
            holder.City = evt.City;
        }
        await db.SaveChangesAsync(cancellationToken);

        if (evt.StartsAt == evt.PreviousStartsAt)
            return;

        foreach (var holder in await RecipientsAsync(db, evt.EventId, cancellationToken))
        {
            var email = EmailTemplates.EventRescheduled(holder.Name, holder.EventTitle, evt.PreviousStartsAt, evt.StartsAt, holder.VenueName, holder.City, _timeZone);
            await SendOnceAsync(db, NotificationType.EventRescheduled, referenceId, holder.CustomerId, holder.Email, email, cancellationToken);
        }
    }

    // Aynı müşterinin aynı etkinlikte birden çok siparişi olabilir; tek e-posta gider. Bütün biletlerini aktaran satın alana
    // yazılmaz.
    private static async Task<IReadOnlyList<TicketHolder>> RecipientsAsync(InboxWorkerDbContext db, Guid eventId, CancellationToken cancellationToken)
        => (await db.TicketHolders.AsNoTracking().Where(h => h.EventId == eventId && h.TicketCount > 0).ToListAsync(cancellationToken))
            .GroupBy(h => h.CustomerId)
            .Select(g => g.First())
            .ToList();

    // Gönderilen her e-posta hemen kaydedilir: (referans, tip, alıcı) tekil olduğu için olay yeniden işlense de aynı kişiye
    // ikinci kez gitmez. Adres yoksa (token'da e-posta yoktu) atlanır. Adres loglanmaz: kişisel veri.
    private async Task SendOnceAsync(
        InboxWorkerDbContext db, NotificationType type, Guid referenceId, string recipientId, string recipientEmail, Email email,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            _logger.LogWarning("E-posta adresi yok, bildirim atlandı: {Type}, alıcı {RecipientId}.", type, recipientId);
            return;
        }

        if (await db.SentNotifications.AnyAsync(
                n => n.ReferenceId == referenceId && n.Type == type && n.RecipientId == recipientId, cancellationToken))
            return;

        await _emailSender.SendAsync(recipientEmail, email.Subject, email.HtmlBody, cancellationToken);

        db.SentNotifications.Add(new SentNotification
        {
            Type = type,
            ReferenceId = referenceId,
            RecipientId = recipientId,
            Subject = email.Subject.Length > 300 ? email.Subject[..300] : email.Subject
        });
        await db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("E-posta gönderildi: {Type}, alıcı {RecipientId}.", type, recipientId);
    }

    // Olayın Kafka mesaj kimliği (gönderen servisin outbox satırı); her tarih değişikliği ayrı bildirimdir.
    private static Guid ReferenceOf(InboxMessage message)
        => Guid.TryParse(message.MessageId, out var id)
            ? id
            : throw new InvalidOperationException($"Mesaj kimliği GUID değil: {message.MessageId}");

    private static T Deserialize<T>(InboxMessage message)
        => JsonSerializer.Deserialize<T>(message.Payload, JsonSerializerOptions.Web)
            ?? throw new InvalidOperationException($"{message.Type} gövdesi okunamadı.");

    // Artan bekleme: 2, 4, 8 … saniye, en fazla 5 dakika; sınır aşılınca dead-letter.
    private async Task RecordFailureAsync(InboxWorkerDbContext db, InboxMessage message, Exception exception, CancellationToken cancellationToken)
    {
        // Kaydedilmemiş değişiklikler bırakılır; kaydedilmiş olanlar (gönderilen e-postalar) transaction'la birlikte kalır.
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
