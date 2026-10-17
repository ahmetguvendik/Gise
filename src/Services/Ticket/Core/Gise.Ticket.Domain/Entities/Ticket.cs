using Gise.Ticket.Domain.Common;
using Gise.Ticket.Domain.Enums;
using Gise.Ticket.Domain.Exceptions;

namespace Gise.Ticket.Domain.Entities;

public sealed class Ticket : BaseEntity
{
    public const int CodeMaxLength = 64;

    public Guid OrderId { get; private set; }
    public Guid EventId { get; private set; }
    public Guid SeatId { get; private set; }
    public string CustomerId { get; private set; } = string.Empty;

    // QR kodun içeriği. Tahmin edilemez olmalı: kapıda okutulan tek şey bu.
    public string Code { get; private set; } = string.Empty;
    public TicketStatus Status { get; private set; }

    // MinIO'daki PDF'in anahtarı; bilet üretildikten sonra dolar.
    public string? PdfKey { get; private set; }
    public DateTime? CheckedInAt { get; private set; }
    public string? CheckedInBy { get; private set; }

    private Ticket() { }

    public static Ticket Create(Guid orderId, Guid eventId, Guid seatId, string customerId, string code)
    {
        if (code.Length is 0 or > CodeMaxLength)
            throw new DomainException("Bilet kodu boş olamaz ve en fazla 64 karakter olabilir.");

        return new Ticket
        {
            OrderId = orderId,
            EventId = eventId,
            SeatId = seatId,
            CustomerId = customerId,
            Code = code,
            Status = TicketStatus.Valid
        };
    }
}
