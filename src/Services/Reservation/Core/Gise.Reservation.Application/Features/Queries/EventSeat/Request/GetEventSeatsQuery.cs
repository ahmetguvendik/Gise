using Gise.Reservation.Application.Features.Queries.EventSeat.Response;
using MediatR;

namespace Gise.Reservation.Application.Features.Queries.EventSeat.Request;

// Koltuk haritası: bölümler, fiyatları ve hangi koltuğun boş olduğu. Herkese açık.
public sealed class GetEventSeatsQuery : IRequest<GetEventSeatsResponse>
{
    public Guid EventId { get; set; }
}
