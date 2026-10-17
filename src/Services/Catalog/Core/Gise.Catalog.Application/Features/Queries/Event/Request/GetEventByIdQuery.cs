using Gise.Catalog.Application.Features.Queries.Event.Response;
using Mediator;

namespace Gise.Catalog.Application.Features.Queries.Event.Request;

public sealed class GetEventByIdQuery : IRequest<GetEventByIdResponse>
{
    public Guid Id { get; set; }
}
