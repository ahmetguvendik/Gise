using Gise.Catalog.Application.Features.Queries.Event.Request;
using Gise.Catalog.Application.Features.Queries.Event.Response;
using Gise.Catalog.Application.Interfaces;
using Gise.Catalog.Domain.Enums;
using Gise.Exceptions;
using MediatR;

namespace Gise.Catalog.Application.Features.Handlers.Event.QueryHandlers;

public sealed class GetEventSeatsQueryHandler : IRequestHandler<GetEventSeatsQuery, GetEventSeatsResponse>
{
    private readonly IEventRepository _eventRepository;
    private readonly IVenueRepository _venueRepository;

    public GetEventSeatsQueryHandler(IEventRepository eventRepository, IVenueRepository venueRepository)
    {
        _eventRepository = eventRepository;
        _venueRepository = venueRepository;
    }

    public async Task<GetEventSeatsResponse> Handle(GetEventSeatsQuery request, CancellationToken cancellationToken)
    {
        var entity = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken)
            ?? throw NotFoundException.Event(request.EventId);

        // Koltuk stoğu yalnızca satışa açılmış etkinlik için kurulur.
        if (entity.Status != EventStatus.Published)
            throw BusinessException.EventNotPublished(entity.Id);

        var venue = await _venueRepository.GetWithLayoutAsync(entity.VenueId, cancellationToken)
            ?? throw NotFoundException.Venue(entity.VenueId);

        // Yayındaki etkinlikte her bölümün fiyatı vardır (yayınlama ve fiyat uçları bunu garanti eder).
        var prices = entity.PriceCategories.ToDictionary(p => p.SectionId, p => p.Price);

        return new GetEventSeatsResponse
        {
            EventId = entity.Id,
            Seats = venue.Sections
                .OrderBy(section => section.Name)
                .SelectMany(section => section.Seats
                    .OrderBy(seat => seat.Row).ThenBy(seat => seat.Number)
                    .Select(seat => new EventSeatResponse
                    {
                        SeatId = seat.Id,
                        SectionId = section.Id,
                        SectionName = section.Name,
                        Row = seat.Row,
                        Number = seat.Number,
                        Price = prices[section.Id]
                    }))
                .ToList()
        };
    }
}
