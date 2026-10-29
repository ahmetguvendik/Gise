using Gise.Payment.Application.Interfaces;
using Gise.Payment.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Gise.Payment.Persistence.Repositories;

public sealed class PaymentRepository : IPaymentRepository
{
    private readonly PaymentDbContext _context;

    public PaymentRepository(PaymentDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Domain.Entities.Payment payment, CancellationToken cancellationToken)
        => await _context.Payments.AddAsync(payment, cancellationToken);

    public Task<Domain.Entities.Payment?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken)
        => _context.Payments.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Domain.Entities.Payment>> GetByOrderIdForUpdateAsync(Guid orderId, CancellationToken cancellationToken)
        => await _context.Payments.Where(p => p.OrderId == orderId).ToListAsync(cancellationToken);
}
