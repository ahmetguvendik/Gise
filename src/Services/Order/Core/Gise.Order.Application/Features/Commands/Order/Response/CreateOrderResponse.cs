namespace Gise.Order.Application.Features.Commands.Order.Response;

public sealed class CreateOrderResponse
{
    public Guid OrderId { get; set; }

    // Hep "Pending": koltuklar arkada tutulur, istemci siparişi sorgulayarak sonucu görür.
    public string Status { get; set; } = string.Empty;
}
