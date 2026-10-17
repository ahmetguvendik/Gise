using Gise.Notification.Domain.Common;
using Gise.Notification.Domain.Enums;

namespace Gise.Notification.Domain.Entities;

// Gönderilmiş bildirimin kaydı. (ReferenceId, Type, RecipientId) tekil: aynı bildirim iki kez gitmez.
public sealed class NotificationLog : BaseEntity
{
    public const int SubjectMaxLength = 300;

    public NotificationType Type { get; private set; }
    public string RecipientId { get; private set; } = string.Empty;
    public string Subject { get; private set; } = string.Empty;

    // Bildirimi tetikleyen kayıt: sipariş ya da bilet grubu.
    public Guid ReferenceId { get; private set; }

    private NotificationLog() { }

    public static NotificationLog Create(NotificationType type, string recipientId, string subject, Guid referenceId) => new()
    {
        Type = type,
        RecipientId = recipientId,
        Subject = subject,
        ReferenceId = referenceId
    };
}
