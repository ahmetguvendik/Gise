using Gise.Catalog.Application.Features.Queries.Event.Response;
using MediatR;

namespace Gise.Catalog.Application.Features.Queries.Event.Request;

public sealed class GetEventByIdQuery : IRequest<GetEventByIdResponse>
{
    public Guid Id { get; set; }
}
