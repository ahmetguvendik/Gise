using Gise.Catalog.Application.Exceptions;
using Gise.Catalog.Application.Features.Queries.Event.Request;
using Gise.Catalog.Application.Features.Queries.Event.Response;
using Gise.Catalog.Application.Interfaces;
using MediatR;

namespace Gise.Catalog.Application.Features.Handlers.Event.QueryHandlers;

public sealed class GetEventByIdQueryHandler : IRequestHandler<GetEventByIdQuery, GetEventByIdResponse>
{
    private readonly IEventRepository _eventRepository;
    private readonly IVenueRepository _venueRepository;

    public GetEventByIdQueryHandler(IEventRepository eventRepository, IVenueRepository venueRepository)
    {
        _eventRepository = eventRepository;
        _venueRepository = venueRepository;
    }

    public async Task<GetEventByIdResponse> Handle(GetEventByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _eventRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Etkinlik bulunamadı.");

        var sectionNames = (await _venueRepository.GetSectionsAsync(entity.VenueId, cancellationToken))
            .ToDictionary(s => s.Id, s => s.Name);

        return new GetEventByIdResponse
        {
            Id = entity.Id,
            Title = entity.Title,
            VenueId = entity.VenueId,
            Status = entity.Status.ToString(),
            StartsAt = entity.StartsAt,
            OrganizerId = entity.OrganizerId,
            CreatedAt = entity.CreatedAt,
            Prices = entity.PriceCategories
                .Select(p => new EventPriceResponse
                {
                    SectionId = p.SectionId,
                    SectionName = sectionNames[p.SectionId],
                    Price = p.Price
                })
                .OrderBy(p => p.SectionName)
                .ToList()
        };
    }
}
