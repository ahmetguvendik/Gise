using Gise.Catalog.Application.Features.Queries.Event.Request;
using Gise.Catalog.Application.Features.Queries.Event.Response;
using Gise.Catalog.Application.Interfaces;

namespace Gise.Catalog.Application.Features.Handlers.Event.QueryHandlers;

public sealed class GetAllEventsQueryHandler
{
    private readonly IEventRepository _eventRepository;
    private readonly TimeProvider _timeProvider;

    public GetAllEventsQueryHandler(IEventRepository eventRepository, TimeProvider timeProvider)
    {
        _eventRepository = eventRepository;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<GetAllEventsResponse>> Handle(GetAllEventsQuery request, CancellationToken cancellationToken)
    {
        var events = await _eventRepository.GetUpcomingPublishedAsync(_timeProvider.GetUtcNow().UtcDateTime, cancellationToken);

        return events
            .Select(e => new GetAllEventsResponse
            {
                Id = e.Id,
                Title = e.Title,
                VenueId = e.VenueId,
                StartsAt = e.StartsAt
            })
            .ToList();
    }
}
