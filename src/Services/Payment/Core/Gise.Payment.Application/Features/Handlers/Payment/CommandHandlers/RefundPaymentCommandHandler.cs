using Gise.Payment.Application.Features.Commands.Payment.Request;
using Gise.Payment.Application.Interfaces;
using Gise.Payment.Application.UnitOfWorks;
using Gise.Payment.Domain.Entities;
using Gise.Payment.Domain.Enums;
using MediatR;

namespace Gise.Payment.Application.Features.Handlers.Payment.CommandHandlers;

public sealed class RefundPaymentCommandHandler : IRequestHandler<RefundPaymentCommand>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IOrderPaymentRepository _orderPaymentRepository;
    private readonly IPaymentProvider _paymentProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public RefundPaymentCommandHandler(
        IPaymentRepository paymentRepository,
        IOrderPaymentRepository orderPaymentRepository,
        IPaymentProvider paymentProvider,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _paymentRepository = paymentRepository;
        _orderPaymentRepository = orderPaymentRepository;
        _paymentProvider = paymentProvider;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task Handle(RefundPaymentCommand request, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        // Önce sipariş kapandı olarak işaretlenir: ödeme aynı anda işleniyorsa bu satırda çakışırlar (bkz. OrderPayment).
        var orderPayment = await _orderPaymentRepository.GetForUpdateAsync(request.OrderId, cancellationToken);
        if (orderPayment is null)
            await _orderPaymentRepository.AddAsync(new OrderPayment { Id = request.OrderId, ClosedAt = now }, cancellationToken);
        else
            orderPayment.ClosedAt ??= now;

        foreach (var payment in await _paymentRepository.GetByOrderIdForUpdateAsync(request.OrderId, cancellationToken))
        {
            if (payment.Status == PaymentStatus.Succeeded)
            {
                // Test sağlayıcısında iade hep başarılı. Hata verirse komut yeniden denenir.
                await _paymentProvider.RefundAsync(payment.Id, payment.Amount, cancellationToken);
                payment.Status = PaymentStatus.Refunded;
                payment.RefundedAt = now;
            }
            else if (payment.Status == PaymentStatus.Pending)
            {
                // Sağlayıcı cevap vermemişti (test kartı 0005); bu deneme artık para çekmez.
                payment.Status = PaymentStatus.Cancelled;
                payment.FailureReason = "Sağlayıcıdan cevap gelmeden sipariş kapandı.";
                payment.CompletedAt = now;
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
