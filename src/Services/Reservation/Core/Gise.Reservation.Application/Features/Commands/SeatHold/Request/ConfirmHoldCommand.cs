using MediatR;

namespace Gise.Reservation.Application.Features.Commands.SeatHold.Request;

// Ödeme alındı: siparişin tuttuğu koltuklar satılır (Order saga'sının ConfirmSeats komutu). Tekrar gelirse zararsızdır.
public sealed class ConfirmHoldCommand : IRequest
{
    public Guid OrderId { get; set; }
}
