using Gise.Payment.Application.Features.Commands.Payment.Request;
using Gise.Payment.Application.Features.Commands.Payment.Response;
using Gise.Payment.Application.Interfaces;
using Gise.Payment.Application.UnitOfWorks;
using Gise.Payment.Domain.Entities;
using Gise.Payment.Domain.Enums;
using MediatR;

namespace Gise.Payment.Application.Features.Handlers.Payment.CommandHandlers;

public sealed class ProcessPaymentCommandHandler : IRequestHandler<ProcessPaymentCommand, ProcessPaymentResponse>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IOrderPaymentRepository _orderPaymentRepository;
    private readonly IPaymentProvider _paymentProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public ProcessPaymentCommandHandler(
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

    public async Task<ProcessPaymentResponse> Handle(ProcessPaymentCommand request, CancellationToken cancellationToken)
    {
        // Aynı deneme ikinci kez gelirse sağlayıcıya yeniden gidilmez, ilk sonuç döner. Cevabı gelmemiş deneme (Pending)
        // sağlayıcıya aynı kimlikle yeniden sorulur.
        var payment = await _paymentRepository.GetForUpdateAsync(request.PaymentId, cancellationToken);
        if (payment is not null && payment.Status != PaymentStatus.Pending)
            return ToResponse(payment);

        if (payment is null)
        {
            payment = new Domain.Entities.Payment
            {
                Id = request.PaymentId,
                OrderId = request.OrderId,
                Amount = request.Amount,
                CardLast4 = request.CardLast4
            };
            await _paymentRepository.AddAsync(payment, cancellationToken);
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var orderPayment = await _orderPaymentRepository.GetForUpdateAsync(request.OrderId, cancellationToken);

        // Sipariş bu arada kapandıysa (RefundPayment önce işlendiyse) para çekilmez.
        if (orderPayment?.ClosedAt is not null)
        {
            payment.Status = PaymentStatus.Cancelled;
            payment.FailureReason = "Sipariş kapandığı için ödeme alınmadı.";
            payment.CompletedAt = now;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return ToResponse(payment);
        }

        var result = await _paymentProvider.ChargeAsync(payment.Id, payment.Amount, payment.CardLast4, cancellationToken);
        switch (result.Outcome)
        {
            case PaymentProviderOutcome.Approved:
                payment.Status = PaymentStatus.Succeeded;
                payment.CompletedAt = now;
                // İade aynı anda işleniyorsa ikisi bu satırda çakışır; yeniden denenen taraf diğerinin sonucunu görür.
                if (orderPayment is null)
                    await _orderPaymentRepository.AddAsync(new OrderPayment { Id = request.OrderId, PaidPaymentId = payment.Id }, cancellationToken);
                else
                    orderPayment.PaidPaymentId = payment.Id;
                break;

            case PaymentProviderOutcome.Declined:
                payment.Status = PaymentStatus.Failed;
                payment.FailureReason = result.DeclineReason ?? "Kart reddedildi.";
                payment.CompletedAt = now;
                break;

            // Cevap gelmedi: deneme Pending kalır, Order'a cevap gitmez; sonucu saga'nın zaman aşımı belirler.
            case PaymentProviderOutcome.NoResponse:
                break;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ToResponse(payment);
    }

    private static ProcessPaymentResponse ToResponse(Domain.Entities.Payment payment)
        => new() { Status = payment.Status, FailureReason = payment.FailureReason };
}
