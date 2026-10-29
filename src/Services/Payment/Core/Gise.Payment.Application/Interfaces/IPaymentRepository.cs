namespace Gise.Payment.Application.Interfaces;

public interface IPaymentRepository
{
    Task AddAsync(Domain.Entities.Payment payment, CancellationToken cancellationToken);

    // Değişiklikler izlenir.
    Task<Domain.Entities.Payment?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken);

    // Siparişin bütün denemeleri; değişiklikler izlenir.
    Task<IReadOnlyList<Domain.Entities.Payment>> GetByOrderIdForUpdateAsync(Guid orderId, CancellationToken cancellationToken);
}
