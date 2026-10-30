using Gise.Catalog.Application.Features.Queries.Event.Response;
using MediatR;

namespace Gise.Catalog.Application.Features.Queries.Event.Request;

// gRPC ucundan gelir (ListPublishedEvents): Search, yayındaki etkinliklerin dizinini bununla baştan kurar.
public sealed class GetPublishedEventsPageQuery : IRequest<GetPublishedEventsPageResponse>
{
    // 0'dan başlar.
    public int Page { get; set; }
    public int PageSize { get; set; }
}
