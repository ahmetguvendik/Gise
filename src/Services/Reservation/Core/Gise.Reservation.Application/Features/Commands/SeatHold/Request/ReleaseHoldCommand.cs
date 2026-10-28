using MediatR;

namespace Gise.Reservation.Application.Features.Commands.SeatHold.Request;

// Siparişin tuttuğu koltukları bırakır (müşteri vazgeçti ya da ödeme süresi doldu). Tutma yoksa ya da zaten
// bırakılmışsa zararsızdır.
public sealed class ReleaseHoldCommand : IRequest
{
    public Guid OrderId { get; set; }

    // Tutma henüz yoksa sipariş bu bilgilerle "bırakıldı" olarak kaydedilir.
    public Guid EventId { get; set; }
    public string CustomerId { get; set; } = string.Empty;
}
