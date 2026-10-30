using Gise.Ticket.Domain.Entities;

namespace Gise.Ticket.Application.Interfaces;

public interface ITicketTransferRepository
{
    Task AddAsync(TicketTransfer transfer, CancellationToken cancellationToken);

    // Değiştirilmek üzere (takip edilir).
    Task<TicketTransfer?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    // Biletin bekleyen aktarımı; değiştirilmek üzere (takip edilir).
    Task<TicketTransfer?> GetPendingForTicketAsync(Guid ticketId, CancellationToken cancellationToken);

    // Salt okunur; Biletlerim'de biletin yanında gösterilir.
    Task<IReadOnlyList<TicketTransfer>> GetPendingForTicketsAsync(IReadOnlyCollection<Guid> ticketIds, CancellationToken cancellationToken);

    // Salt okunur: e-posta adresine gönderilmiş bekleyen aktarımlar ve biletleri, en yakın etkinlik önce.
    Task<IReadOnlyList<(TicketTransfer Transfer, Domain.Entities.Ticket Ticket)>> GetPendingForEmailAsync(string email, CancellationToken cancellationToken);
}
