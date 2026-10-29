using Gise.Ticket.Domain.Common;
using Gise.Ticket.Domain.Enums;

namespace Gise.Ticket.Domain.Entities;

// Bir koltuğun bileti. Satın alma saga'sı koltuklar satılınca çıkarır (IssueTickets); kapıda Code okutulur.
public class Ticket : BaseEntity
{
    public Guid OrderId { get; set; }
    public Guid EventId { get; set; }
    public Guid SeatId { get; set; }

    // Keycloak'taki müşteri (sub).
    public string CustomerId { get; set; } = string.Empty;

    // Biletlerim ve PDF Catalog'a sormadan gösterilsin diye siparişten kopyalanır.
    public string EventTitle { get; set; } = string.Empty;
    public DateTime EventStartsAt { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string SectionName { get; set; } = string.Empty;
    public string Row { get; set; } = string.Empty;
    public int Number { get; set; }
    public decimal Price { get; set; }

    // QR kodun içeriği. Tahmin edilemez olmalı: kapıda okutulan tek şey bu.
    public string Code { get; set; } = string.Empty;
    public TicketStatus Status { get; set; } = TicketStatus.Valid;

    // MinIO'daki PDF'in anahtarı (gise-media bucket'ında, herkese açık olmayan tickets/ altında).
    public string PdfKey { get; set; } = string.Empty;
    public DateTime? CheckedInAt { get; set; }

    // Okutan görevlinin Keycloak kimliği (sub).
    public string? CheckedInBy { get; set; }
}
