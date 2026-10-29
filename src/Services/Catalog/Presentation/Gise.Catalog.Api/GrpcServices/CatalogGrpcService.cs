using Gise.Catalog.Application.Features.Queries.Event.Request;
using Gise.Contracts.Grpc.Catalog;
using Gise.Exceptions;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;

namespace Gise.Catalog.Api.GrpcServices;

// Servisler arası senkron uç (sözleşme: Gise.Contracts/Protos/catalog.proto). Yalnızca 8081'den,
// Docker ağının içinden erişilir; gateway'e açık değil.
public sealed class CatalogGrpcService : CatalogGrpc.CatalogGrpcBase
{
    private readonly IMediator _mediator;

    public CatalogGrpcService(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<GetEventSeatsReply> GetEventSeats(GetEventSeatsRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.EventId, out var eventId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "event_id geçerli bir GUID değil."));

        try
        {
            var result = await _mediator.Send(new GetEventSeatsQuery { EventId = eventId }, context.CancellationToken);

            var reply = new GetEventSeatsReply { EventId = result.EventId.ToString() };
            reply.Seats.AddRange(result.Seats.Select(seat => new SeatItem
            {
                SeatId = seat.SeatId.ToString(),
                SectionId = seat.SectionId.ToString(),
                SectionName = seat.SectionName,
                Row = seat.Row,
                Number = seat.Number,
                PriceKurus = decimal.ToInt64(seat.Price * 100)
            }));

            return reply;
        }
        catch (AppException ex)
        {
            throw ToRpcException(ex);
        }
    }

    public override async Task<GetEventSummaryReply> GetEventSummary(GetEventSummaryRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.EventId, out var eventId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "event_id geçerli bir GUID değil."));

        try
        {
            var result = await _mediator.Send(new GetEventSummaryQuery { EventId = eventId }, context.CancellationToken);

            return new GetEventSummaryReply
            {
                EventId = result.EventId.ToString(),
                Title = result.Title,
                StartsAt = Timestamp.FromDateTime(DateTime.SpecifyKind(result.StartsAt, DateTimeKind.Utc)),
                VenueName = result.VenueName,
                City = result.City
            };
        }
        catch (AppException ex)
        {
            throw ToRpcException(ex);
        }
    }

    // Merkezi hatalar gRPC durum koduna çevrilir; error-code başlığı merkezi kodu taşır, çağıran servis ona göre davranır.
    private static RpcException ToRpcException(AppException ex)
    {
        var code = ex.StatusCode switch
        {
            StatusCodes.Status400BadRequest => StatusCode.InvalidArgument,
            StatusCodes.Status403Forbidden => StatusCode.PermissionDenied,
            StatusCodes.Status404NotFound => StatusCode.NotFound,
            _ => StatusCode.FailedPrecondition
        };
        return new RpcException(new Status(code, ex.Message), new Metadata { { "error-code", ex.ErrorCode } });
    }
}
