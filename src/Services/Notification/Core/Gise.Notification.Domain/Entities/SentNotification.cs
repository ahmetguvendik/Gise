using Gise.Notification.Domain.Common;
using Gise.Notification.Domain.Enums;

namespace Gise.Notification.Domain.Entities;

// Gönderilmiş bir bildirim. Uygulama logu değil, iş kaydıdır: (ReferenceId, Type, RecipientId)
// tekil olduğu için aynı olay iki kez gelse de müşteriye ikinci bildirim gitmez.
public class SentNotification : BaseEntity
{
    public NotificationType Type { get; set; }
    public string RecipientId { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;

    // Bildirimi tetikleyen kayıt (bkz. NotificationType).
    public Guid ReferenceId { get; set; }
}
