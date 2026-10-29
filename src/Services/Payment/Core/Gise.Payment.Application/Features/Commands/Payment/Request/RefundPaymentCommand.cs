using MediatR;

namespace Gise.Payment.Application.Features.Commands.Payment.Request;

// Sipariş kapandı (Order'ın RefundPayment komutu): çekilen para iade edilir, cevabı beklenen deneme iptal edilir,
// bundan sonra gelen ödeme isteği para çekmez. Tekrar gelirse zararsızdır.
public sealed class RefundPaymentCommand : IRequest
{
    public Guid OrderId { get; set; }
}
