using Gise.Catalog.Application.Features.Queries.Venue.Request;
using Gise.Catalog.Application.Features.Queries.Venue.Response;
using Gise.Catalog.Application.Interfaces;

namespace Gise.Catalog.Application.Features.Handlers.Venue.QueryHandlers;

public sealed class GetAllVenuesQueryHandler
{
    private readonly IVenueRepository _venueRepository;

    public GetAllVenuesQueryHandler(IVenueRepository venueRepository)
    {
        _venueRepository = venueRepository;
    }

    public async Task<IReadOnlyList<GetAllVenuesResponse>> Handle(GetAllVenuesQuery request, CancellationToken cancellationToken)
    {
        var venues = await _venueRepository.GetAllAsync(cancellationToken);

        return venues
            .Select(v => new GetAllVenuesResponse { Id = v.Id, Name = v.Name, City = v.City })
            .ToList();
    }
}
