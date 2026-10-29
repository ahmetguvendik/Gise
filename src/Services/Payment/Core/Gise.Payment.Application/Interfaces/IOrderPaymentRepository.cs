using Gise.Payment.Domain.Entities;

namespace Gise.Payment.Application.Interfaces;

public interface IOrderPaymentRepository
{
    Task AddAsync(OrderPayment orderPayment, CancellationToken cancellationToken);

    // Değişiklikler izlenir.
    Task<OrderPayment?> GetForUpdateAsync(Guid orderId, CancellationToken cancellationToken);
}
