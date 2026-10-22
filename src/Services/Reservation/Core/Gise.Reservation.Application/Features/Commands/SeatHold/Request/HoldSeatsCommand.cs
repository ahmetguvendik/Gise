using Gise.Reservation.Application.Features.Commands.SeatHold.Response;
using MediatR;
using System.Text.Json.Serialization;

namespace Gise.Reservation.Application.Features.Commands.SeatHold.Request;

// Seçilen koltukları süreli tutar. Order gelince RabbitMQ'daki ReserveSeats de bu komutu çağıracak.
public sealed class HoldSeatsCommand : IRequest<HoldSeatsResponse>
{
    public Guid EventId { get; set; }
    public List<Guid> SeatIds { get; set; } = [];

    // Body'den gelmez — controller JWT'den set eder.
    [JsonIgnore]
    public string UserId { get; set; } = string.Empty;
}
