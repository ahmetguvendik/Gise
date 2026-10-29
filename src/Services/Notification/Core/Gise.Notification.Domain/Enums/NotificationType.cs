namespace Gise.Notification.Domain.Enums;

public enum NotificationType
{
    // Biletler çıktı; ReferenceId siparişin ID'si.
    TicketsIssued,

    // Etkinlik iptal edildi; ReferenceId etkinliğin ID'si.
    EventCancelled,

    // Etkinliğin tarihi değişti; ReferenceId olayın Kafka mesaj kimliği (her erteleme ayrı bildirim).
    EventRescheduled
}
