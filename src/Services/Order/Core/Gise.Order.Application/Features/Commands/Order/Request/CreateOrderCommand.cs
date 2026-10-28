using Gise.Order.Application.Features.Commands.Order.Response;
using MediatR;
using System.Text.Json.Serialization;

namespace Gise.Order.Application.Features.Commands.Order.Request;

// Müşteri koltukları seçip satın almaya başlar. Sipariş "Pending" açılır; koltukları saga tutturur.
public sealed class CreateOrderCommand : IRequest<CreateOrderResponse>
{
    public Guid EventId { get; set; }
    public List<Guid> SeatIds { get; set; } = [];

    // Body'den gelmez — controller JWT'den set eder.
    [JsonIgnore]
    public string CustomerId { get; set; } = string.Empty;
}
