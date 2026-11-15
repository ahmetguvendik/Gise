using Gise.Catalog.Application.Caching;
using Gise.Catalog.Application.Features.Queries.Event.Response;
using MediatR;

namespace Gise.Catalog.Application.Features.Queries.Event.Request;

// gRPC ucundan gelir (GetEventSummary): Order, sipariş açarken etkinliğin adını, tarihini ve mekânını kopyalar.
public sealed class GetEventSummaryQuery : IRequest<GetEventSummaryResponse>, ICacheableQuery
{
    public Guid EventId { get; set; }

    // Her siparişte sorulur. Yalnızca satıştaki etkinlik için başarılı yanıt yazılır; "başladı mı" kontrolü önbellekte
    // değil, gRPC servisinde her çağrıda yapılır.
    public string CacheKey => CacheKeys.EventSummary(EventId);

    public TimeSpan Duration => TimeSpan.FromMinutes(5);
}
