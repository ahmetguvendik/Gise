using Gise.Catalog.Application.Features.Queries.Venue.Request;
using Gise.Catalog.Application.Features.Queries.Venue.Response;
using Gise.Catalog.Application.Interfaces;
using MediatR;

namespace Gise.Catalog.Application.Features.Handlers.Venue.QueryHandlers;

public sealed class GetAllVenuesQueryHandler : IRequestHandler<GetAllVenuesQuery, IReadOnlyList<GetAllVenuesResponse>>
{
    private readonly IVenueRepository _venueRepository;
    private readonly IEventRepository _eventRepository;

    public GetAllVenuesQueryHandler(IVenueRepository venueRepository, IEventRepository eventRepository)
    {
        _venueRepository = venueRepository;
        _eventRepository = eventRepository;
    }

    public async Task<IReadOnlyList<GetAllVenuesResponse>> Handle(GetAllVenuesQuery request, CancellationToken cancellationToken)
    {
        var venues = await _venueRepository.GetAllAsync(cancellationToken);
        var stats = await _venueRepository.GetLayoutStatsAsync(cancellationToken);
        var inUse = await _eventRepository.GetVenueIdsWithEventsAsync(cancellationToken);

        return venues
            .Select(v => new GetAllVenuesResponse
            {
                Id = v.Id,
                Name = v.Name,
                City = v.City,
                SectionCount = stats.GetValueOrDefault(v.Id).SectionCount,
                SeatCount = stats.GetValueOrDefault(v.Id).SeatCount,
                HasEvents = inUse.Contains(v.Id)
            })
            .ToList();
    }
}
