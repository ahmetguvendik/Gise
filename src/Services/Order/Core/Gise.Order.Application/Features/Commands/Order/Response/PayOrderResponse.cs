namespace Gise.Order.Application.Features.Commands.Order.Response;

public sealed class PayOrderResponse
{
    // Ödeme arkada yapılır; sonucu siparişin durumu gösterir.
    public Guid PaymentId { get; set; }
}
