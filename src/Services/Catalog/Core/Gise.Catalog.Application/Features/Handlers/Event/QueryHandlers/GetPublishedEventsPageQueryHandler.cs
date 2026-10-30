using Gise.Catalog.Application.Features.Queries.Event.Request;
using Gise.Catalog.Application.Features.Queries.Event.Response;
using Gise.Catalog.Application.Interfaces;
using MediatR;

namespace Gise.Catalog.Application.Features.Handlers.Event.QueryHandlers;

public sealed class GetPublishedEventsPageQueryHandler : IRequestHandler<GetPublishedEventsPageQuery, GetPublishedEventsPageResponse>
{
    private readonly IEventRepository _eventRepository;
    private readonly IVenueRepository _venueRepository;
    private readonly TimeProvider _timeProvider;

    public GetPublishedEventsPageQueryHandler(IEventRepository eventRepository, IVenueRepository venueRepository, TimeProvider timeProvider)
    {
        _eventRepository = eventRepository;
        _venueRepository = venueRepository;
        _timeProvider = timeProvider;
    }

    public async Task<GetPublishedEventsPageResponse> Handle(GetPublishedEventsPageQuery request, CancellationToken cancellationToken)
    {
        // Bir fazlası istenir: sonraki sayfa var mı, ayrıca saymadan anlaşılır.
        var events = await _eventRepository.GetUpcomingPublishedPageAsync(
            _timeProvider.GetUtcNow().UtcDateTime, request.Page * request.PageSize, request.PageSize + 1, cancellationToken);

        // Mekân sayısı az; hepsi bir kez okunur.
        var venues = (await _venueRepository.GetAllAsync(cancellationToken)).ToDictionary(v => v.Id);

        return new GetPublishedEventsPageResponse
        {
            HasMore = events.Count > request.PageSize,
            Events = events
                .Take(request.PageSize)
                .Select(e => new PublishedEventResponse
                {
                    EventId = e.Id,
                    VenueId = e.VenueId,
                    Title = e.Title,
                    StartsAt = e.StartsAt,
                    VenueName = venues[e.VenueId].Name,
                    City = venues[e.VenueId].City,
                    // Yayındaki etkinlikte her bölümün fiyatı vardır (yayınlama bunu garanti eder).
                    MinPrice = e.PriceCategories.Min(p => p.Price),
                    PosterKey = e.PosterKey
                })
                .ToList()
        };
    }
}
