namespace Gise.Ticket.Domain.Enums;

public enum TicketTransferStatus
{
    Pending,
    Accepted,
    // Alıcı reddetti.
    Declined,
    // Gönderen kabul edilmeden geri aldı.
    Cancelled
}
