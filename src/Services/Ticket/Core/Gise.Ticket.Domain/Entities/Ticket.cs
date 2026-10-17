using Gise.Ticket.Domain.Common;
using Gise.Ticket.Domain.Enums;

namespace Gise.Ticket.Domain.Entities;

public class Ticket : BaseEntity
{
    public Guid OrderId { get; set; }
    public Guid EventId { get; set; }
    public Guid SeatId { get; set; }
    public string CustomerId { get; set; } = string.Empty;

    // QR kodun içeriği. Tahmin edilemez olmalı: kapıda okutulan tek şey bu.
    public string Code { get; set; } = string.Empty;
    public TicketStatus Status { get; set; } = TicketStatus.Valid;

    // MinIO'daki PDF'in anahtarı; bilet üretildikten sonra dolar.
    public string? PdfKey { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public string? CheckedInBy { get; set; }
}
