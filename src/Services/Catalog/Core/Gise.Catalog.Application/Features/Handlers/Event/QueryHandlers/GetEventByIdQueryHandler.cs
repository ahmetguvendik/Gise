using Gise.Catalog.Application.Exceptions;
using Gise.Catalog.Application.Features.Queries.Event.Request;
using Gise.Catalog.Application.Features.Queries.Event.Response;
using Gise.Catalog.Application.Interfaces;

namespace Gise.Catalog.Application.Features.Handlers.Event.QueryHandlers;

public sealed class GetEventByIdQueryHandler
{
    private readonly IEventRepository _eventRepository;

    public GetEventByIdQueryHandler(IEventRepository eventRepository)
    {
        _eventRepository = eventRepository;
    }

    public async Task<GetEventByIdResponse> Handle(GetEventByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _eventRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Etkinlik bulunamadı.");

        return new GetEventByIdResponse
        {
            Id = entity.Id,
            Title = entity.Title,
            VenueId = entity.VenueId,
            Status = entity.Status.ToString(),
            StartsAt = entity.StartsAt,
            OrganizerId = entity.OrganizerId,
            CreatedAt = entity.CreatedAt
        };
    }
}
