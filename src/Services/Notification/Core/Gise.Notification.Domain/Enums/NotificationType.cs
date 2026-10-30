namespace Gise.Notification.Domain.Enums;

public enum NotificationType
{
    // Biletler çıktı; ReferenceId siparişin ID'si.
    TicketsIssued,

    // Etkinlik iptal edildi; ReferenceId etkinliğin ID'si.
    EventCancelled,

    // Etkinliğin tarihi değişti; ReferenceId olayın Kafka mesaj kimliği (her erteleme ayrı bildirim).
    EventRescheduled,

    // Bilet bir e-postaya aktarılmak istendi, alıcıya yazıldı; ReferenceId aktarımın ID'si.
    TicketTransferOffered,

    // Aktarım kabul edildi, gönderene yazıldı; ReferenceId aktarımın ID'si.
    TicketTransferAccepted
}
