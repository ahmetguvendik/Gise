namespace Gise.Ticket.Application.Interfaces;

public interface ITicketRepository
{
    Task AddRangeAsync(IEnumerable<Domain.Entities.Ticket> tickets, CancellationToken cancellationToken);

    Task<bool> AnyForOrderAsync(Guid orderId, CancellationToken cancellationToken);

    // Koltuklardan birinin iptal edilmemiş bileti var mı.
    Task<bool> AnyActiveForSeatsAsync(Guid eventId, IReadOnlyCollection<Guid> seatIds, CancellationToken cancellationToken);

    // Salt okunur.
    Task<Domain.Entities.Ticket?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    // Salt okunur; kod normalize edilmiş olmalı (bkz. TicketCode).
    Task<Domain.Entities.Ticket?> GetByCodeAsync(string code, CancellationToken cancellationToken);

    // Müşterinin biletleri, en yakın etkinlik önce; sipariş verilirse yalnızca onunkiler.
    Task<IReadOnlyList<Domain.Entities.Ticket>> GetByCustomerAsync(string customerId, Guid? orderId, CancellationToken cancellationToken);

    // Bilet hâlâ geçerliyse tek komutta "kullanıldı" yapar. Aynı bilet aynı anda iki kapıda okutulursa yalnızca biri true alır.
    Task<bool> TryCheckInAsync(Guid id, string staffId, DateTime checkedInAt, CancellationToken cancellationToken);

    // Kapıda yakalanan, etkinliği iptal edilmiş ama hâlâ geçerli görünen bilet.
    Task CancelAsync(Guid id, CancellationToken cancellationToken);
}
