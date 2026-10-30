using Gise.Contracts.Grpc.Reservation;
using Gise.Reservation.Application.Features.Queries.EventStock.Request;
using Grpc.Core;
using MediatR;

namespace Gise.Reservation.Api.GrpcServices;

// Servisler arası senkron uç (sözleşme: Gise.Contracts/Protos/reservation.proto). Yalnızca 8081'den, Docker ağının
// içinden erişilir; gateway'e açık değil.
public sealed class ReservationGrpcService : ReservationGrpc.ReservationGrpcBase
{
    // Çağıranın büyük istek atması Reservation'ı yormasın; Search 200'erli gönderir.
    private const int MaxEventIds = 500;

    private readonly IMediator _mediator;

    public ReservationGrpcService(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<GetAvailabilityReply> GetAvailability(GetAvailabilityRequest request, ServerCallContext context)
    {
        if (request.EventIds.Count > MaxEventIds)
            throw new RpcException(new Status(StatusCode.InvalidArgument, $"Bir istekte en fazla {MaxEventIds} etkinlik."));

        var eventIds = new List<Guid>(request.EventIds.Count);
        foreach (var id in request.EventIds)
        {
            if (!Guid.TryParse(id, out var eventId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, $"event_ids içinde geçerli olmayan GUID: {id}"));
            eventIds.Add(eventId);
        }

        var result = await _mediator.Send(new GetEventAvailabilityQuery { EventIds = eventIds }, context.CancellationToken);

        var reply = new GetAvailabilityReply();
        reply.Events.AddRange(result.Select(a => new EventAvailability
        {
            EventId = a.EventId.ToString(),
            SeatCount = a.SeatCount,
            AvailableCount = a.AvailableCount
        }));
        return reply;
    }
}
