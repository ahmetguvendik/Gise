using Gise.Payment.Application.Interfaces;
using Gise.Payment.Domain.Entities;
using Gise.Payment.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Gise.Payment.Persistence.Repositories;

public sealed class OrderPaymentRepository : IOrderPaymentRepository
{
    private readonly PaymentDbContext _context;

    public OrderPaymentRepository(PaymentDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(OrderPayment orderPayment, CancellationToken cancellationToken)
        => await _context.OrderPayments.AddAsync(orderPayment, cancellationToken);

    public Task<OrderPayment?> GetForUpdateAsync(Guid orderId, CancellationToken cancellationToken)
        => _context.OrderPayments.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
}
