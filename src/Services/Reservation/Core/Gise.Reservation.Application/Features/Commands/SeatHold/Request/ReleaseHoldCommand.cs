using MediatR;

namespace Gise.Reservation.Application.Features.Commands.SeatHold.Request;

// Tutulan koltukları süresi dolmadan bırakır. Bırakılmış ya da süresi dolmuş tutmayı bırakmak zararsızdır.
public sealed class ReleaseHoldCommand : IRequest
{
    public Guid HoldId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
}
