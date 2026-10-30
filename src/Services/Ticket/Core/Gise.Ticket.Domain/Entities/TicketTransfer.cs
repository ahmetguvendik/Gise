using Gise.Ticket.Domain.Common;
using Gise.Ticket.Domain.Enums;

namespace Gise.Ticket.Domain.Entities;

// Bir biletin aktarım teklifi. Sahibi alıcının e-postasını yazar; alıcı o e-postayla giriş yapıp kabul edince bilet ona
// geçer (yeni kod ve PDF). Kabul edilene kadar gönderen geri alabilir, alıcı reddedebilir. Etkinliğe belli bir süre
// kala (TicketTransfer:CutoffHours) bekleyen teklif düşer; bu durum saklanmaz, etkinliğin güncel tarihinden hesaplanır.
public class TicketTransfer : BaseEntity
{
    public Guid TicketId { get; set; }
    public Guid EventId { get; set; }

    // Gönderen (Keycloak sub); adı ve e-postası token'dan: alıcıya kimden geldiği, gönderene kabul bildirimi.
    public string FromCustomerId { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;

    // Küçük harf; alıcının token'ındaki doğrulanmış e-postayla eşleşir.
    public string ToEmail { get; set; } = string.Empty;

    public TicketTransferStatus Status { get; set; } = TicketTransferStatus.Pending;

    // Kabul eden (Keycloak sub).
    public string? ToCustomerId { get; set; }

    // Kabul, ret ya da geri alma zamanı.
    public DateTime? RespondedAt { get; set; }

    // PostgreSQL xmin: kabul ile geri alma aynı anda gelirse yalnızca biri geçer.
    public uint Version { get; set; }
}
