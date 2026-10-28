namespace Gise.Order.Application.Interfaces;

public interface IOrderRepository
{
    Task AddAsync(Domain.Entities.Order order, CancellationToken cancellationToken);

    // Koltuklarıyla birlikte, salt okunur.
    Task<Domain.Entities.Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    // Koltuklarıyla birlikte, değişiklikler izlenir.
    Task<Domain.Entities.Order?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken);

    // Müşterinin siparişleri, en yenisi önce.
    Task<IReadOnlyList<Domain.Entities.Order>> GetByCustomerAsync(string customerId, CancellationToken cancellationToken);
}
