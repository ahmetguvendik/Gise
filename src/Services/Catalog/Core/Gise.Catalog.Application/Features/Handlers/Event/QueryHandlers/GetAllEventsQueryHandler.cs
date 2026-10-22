using Gise.Catalog.Application.Features.Queries.Event.Request;
using Gise.Catalog.Application.Features.Queries.Event.Response;
using Gise.Catalog.Application.Interfaces;
using MediatR;

namespace Gise.Catalog.Application.Features.Handlers.Event.QueryHandlers;

public sealed class GetAllEventsQueryHandler : IRequestHandler<GetAllEventsQuery, IReadOnlyList<GetAllEventsResponse>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IStorageService _storageService;
    private readonly TimeProvider _timeProvider;

    public GetAllEventsQueryHandler(IEventRepository eventRepository, IStorageService storageService, TimeProvider timeProvider)
    {
        _eventRepository = eventRepository;
        _storageService = storageService;
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
                StartsAt = e.StartsAt,
                PosterUrl = _storageService.ResolvePublicUrl(e.PosterKey),
                PosterSrcset = _storageService.ResolveSrcset(e.PosterKey),
                MinPrice = e.PriceCategories.Count > 0 ? e.PriceCategories.Min(p => p.Price) : null
            })
            .ToList();
    }
}
