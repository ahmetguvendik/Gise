using Gise.Notification.Domain.Common;
using Gise.Notification.Domain.Enums;

namespace Gise.Notification.Domain.Entities;

// Gönderilmiş bir bildirim. Uygulama logu değil, iş kaydıdır: (ReferenceId, Type, RecipientId)
// tekil olduğu için aynı olay iki kez gelse de müşteriye ikinci bildirim gitmez.
public sealed class SentNotification : BaseEntity
{
    public const int SubjectMaxLength = 300;

    public NotificationType Type { get; private set; }
    public string RecipientId { get; private set; } = string.Empty;
    public string Subject { get; private set; } = string.Empty;

    // Bildirimi tetikleyen kayıt: sipariş ya da bilet grubu.
    public Guid ReferenceId { get; private set; }

    private SentNotification() { }

    public static SentNotification Create(NotificationType type, string recipientId, string subject, Guid referenceId) => new()
    {
        Type = type,
        RecipientId = recipientId,
        Subject = subject,
        ReferenceId = referenceId
    };
}
