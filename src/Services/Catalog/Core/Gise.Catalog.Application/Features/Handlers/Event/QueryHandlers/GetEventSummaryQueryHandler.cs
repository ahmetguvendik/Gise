using Gise.Catalog.Application.Features.Queries.Event.Request;
using Gise.Catalog.Application.Features.Queries.Event.Response;
using Gise.Catalog.Application.Interfaces;
using Gise.Catalog.Domain.Enums;
using Gise.Exceptions;
using MediatR;

namespace Gise.Catalog.Application.Features.Handlers.Event.QueryHandlers;

public sealed class GetEventSummaryQueryHandler : IRequestHandler<GetEventSummaryQuery, GetEventSummaryResponse>
{
    private readonly IEventRepository _eventRepository;
    private readonly IVenueRepository _venueRepository;
    private readonly TimeProvider _timeProvider;

    public GetEventSummaryQueryHandler(IEventRepository eventRepository, IVenueRepository venueRepository, TimeProvider timeProvider)
    {
        _eventRepository = eventRepository;
        _venueRepository = venueRepository;
        _timeProvider = timeProvider;
    }

    public async Task<GetEventSummaryResponse> Handle(GetEventSummaryQuery request, CancellationToken cancellationToken)
    {
        var entity = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken)
            ?? throw NotFoundException.Event(request.EventId);

        // Yalnızca satıştaki etkinliğe sipariş açılır.
        if (entity.Status == EventStatus.Cancelled)
            throw BusinessException.EventCancelled(entity.Id);

        if (entity.Status != EventStatus.Published)
            throw BusinessException.EventNotPublished(entity.Id);

        if (entity.StartsAt <= _timeProvider.GetUtcNow().UtcDateTime)
            throw BusinessException.EventStarted(entity.Id);

        var venue = await _venueRepository.GetByIdAsync(entity.VenueId, cancellationToken)
            ?? throw NotFoundException.Venue(entity.VenueId);

        return new GetEventSummaryResponse
        {
            EventId = entity.Id,
            Title = entity.Title,
            StartsAt = entity.StartsAt,
            VenueName = venue.Name,
            City = venue.City
        };
    }
}
