namespace Gise.Ticket.Application.Interfaces;

// Biletin tek sayfalık PDF'i: etkinlik, koltuk, QR kod.
public interface ITicketPdfRenderer
{
    byte[] Render(Domain.Entities.Ticket ticket);
}
