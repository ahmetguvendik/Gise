using Gise.Catalog.Application.Features.Queries.Event.Response;
using MediatR;

namespace Gise.Catalog.Application.Features.Queries.Event.Request;

// gRPC ucundan gelir (GetEventSummary): Order, sipariş açarken etkinliğin adını, tarihini ve mekânını kopyalar.
public sealed class GetEventSummaryQuery : IRequest<GetEventSummaryResponse>
{
    public Guid EventId { get; set; }
}
