using Gise.Order.Application.Features.Commands.Order.Response;
using MediatR;
using System.Text.Json.Serialization;

namespace Gise.Order.Application.Features.Commands.Order.Request;

// Müşteri ödeme beklenirken kart bilgisini girip öder. Kart burada yalnızca doğrulanır; hiçbir yere yazılmaz,
// Payment'a son dört hanesi gider.
public sealed class PayOrderCommand : IRequest<PayOrderResponse>
{
    // Boşluksuz 16 hane.
    public string CardNumber { get; set; } = string.Empty;
    public int ExpiryMonth { get; set; }

    // Dört haneli yıl.
    public int ExpiryYear { get; set; }
    public string Cvc { get; set; } = string.Empty;

    // Body'den gelmez — controller route'tan ve JWT'den set eder.
    [JsonIgnore]
    public Guid OrderId { get; set; }

    [JsonIgnore]
    public string CustomerId { get; set; } = string.Empty;
}
