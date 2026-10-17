using Gise.Catalog.Application.Features.Queries.Venue.Request;
using Gise.Catalog.Application.Features.Queries.Venue.Response;
using Gise.Catalog.Application.Interfaces;
using Gise.Exceptions;
using MediatR;

namespace Gise.Catalog.Application.Features.Handlers.Venue.QueryHandlers;

public sealed class GetVenueByIdQueryHandler : IRequestHandler<GetVenueByIdQuery, GetVenueByIdResponse>
{
    private readonly IVenueRepository _venueRepository;

    public GetVenueByIdQueryHandler(IVenueRepository venueRepository)
    {
        _venueRepository = venueRepository;
    }

    public async Task<GetVenueByIdResponse> Handle(GetVenueByIdQuery request, CancellationToken cancellationToken)
    {
        var venue = await _venueRepository.GetWithLayoutAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.Venue(request.Id);

        return new GetVenueByIdResponse
        {
            Id = venue.Id,
            Name = venue.Name,
            City = venue.City,
            Sections = venue.Sections
                .OrderBy(s => s.Name)
                .Select(s => new VenueSectionResponse
                {
                    Id = s.Id,
                    Name = s.Name,
                    Rows = s.Seats
                        .GroupBy(seat => seat.Row)
                        .OrderBy(g => g.Key)
                        .Select(g => new VenueRowResponse { Row = g.Key, SeatCount = g.Count() })
                        .ToList()
                })
                .ToList()
        };
    }
}
