using Gise.Catalog.Application.Features.Queries.Event.Request;
using Gise.Catalog.Application.Features.Queries.Event.Response;
using Gise.Catalog.Application.Interfaces;
using MediatR;

namespace Gise.Catalog.Application.Features.Handlers.Event.QueryHandlers;

public sealed class GetPendingEventsQueryHandler : IRequestHandler<GetPendingEventsQuery, IReadOnlyList<GetPendingEventsResponse>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IVenueRepository _venueRepository;
    private readonly IStorageService _storageService;

    public GetPendingEventsQueryHandler(IEventRepository eventRepository, IVenueRepository venueRepository, IStorageService storageService)
    {
        _eventRepository = eventRepository;
        _venueRepository = venueRepository;
        _storageService = storageService;
    }

    public async Task<IReadOnlyList<GetPendingEventsResponse>> Handle(GetPendingEventsQuery request, CancellationToken cancellationToken)
    {
        var events = await _eventRepository.GetPendingReviewAsync(cancellationToken);
        var venues = (await _venueRepository.GetAllAsync(cancellationToken)).ToDictionary(v => v.Id);

        return events
            .Select(e => new GetPendingEventsResponse
            {
                Id = e.Id,
                Title = e.Title,
                Status = e.Status.ToString(),
                StartsAt = e.StartsAt,
                PosterUrl = _storageService.ResolvePublicUrl(e.PosterKey),
                PosterSrcset = _storageService.ResolveSrcset(e.PosterKey),
                VenueId = e.VenueId,
                VenueName = venues[e.VenueId].Name,
                City = venues[e.VenueId].City,
                MinPrice = e.PriceCategories.Count > 0 ? e.PriceCategories.Min(p => p.Price) : null
            })
            .ToList();
    }
}
