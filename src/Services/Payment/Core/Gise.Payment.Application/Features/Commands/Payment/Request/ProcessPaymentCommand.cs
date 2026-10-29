using Gise.Payment.Application.Features.Commands.Payment.Response;
using MediatR;

namespace Gise.Payment.Application.Features.Commands.Payment.Request;

// Bir ödeme denemesinin parasını çeker (Order'ın ProcessPayment komutu).
public sealed class ProcessPaymentCommand : IRequest<ProcessPaymentResponse>
{
    public Guid PaymentId { get; set; }
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public string CardLast4 { get; set; } = string.Empty;
}
