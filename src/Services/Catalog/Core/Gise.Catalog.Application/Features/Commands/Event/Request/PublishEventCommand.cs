using Gise.Catalog.Application.Features.Commands.Event.Response;
using MediatR;

namespace Gise.Catalog.Application.Features.Commands.Event.Request;

// Yalnızca admin; taslak ya da onay bekleyen etkinlik yayınlanır.
public sealed class PublishEventCommand : IRequest<PublishEventResponse>
{
    public Guid EventId { get; set; }
}
