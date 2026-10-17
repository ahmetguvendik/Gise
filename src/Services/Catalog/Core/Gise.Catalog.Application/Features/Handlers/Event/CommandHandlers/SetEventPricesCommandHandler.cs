using Gise.Catalog.Application.Exceptions;
using Gise.Catalog.Application.Features.Commands.Event.Request;
using Gise.Catalog.Application.Features.Commands.Event.Response;
using Gise.Catalog.Application.Interfaces;
using Gise.Catalog.Application.UnitOfWorks;
using MediatR;

namespace Gise.Catalog.Application.Features.Handlers.Event.CommandHandlers;

public sealed class SetEventPricesCommandHandler : IRequestHandler<SetEventPricesCommand, SetEventPricesResponse>
{
    private readonly IEventRepository _eventRepository;
    private readonly IVenueRepository _venueRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SetEventPricesCommandHandler(
        IEventRepository eventRepository,
        IVenueRepository venueRepository,
        IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _venueRepository = venueRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<SetEventPricesResponse> Handle(SetEventPricesCommand request, CancellationToken cancellationToken)
    {
        var entity = await _eventRepository.GetForUpdateAsync(request.EventId, cancellationToken)
            ?? throw new NotFoundException("Etkinlik bulunamadı.");

        // Organizatör yalnızca kendi etkinliğinin fiyatlarını belirler; admin hepsini.
        if (!request.IsAdmin && entity.OrganizerId != request.UserId)
            throw new ForbiddenException("Yalnızca kendi etkinliğinizin fiyatlarını değiştirebilirsiniz.");

        var sections = await _venueRepository.GetSectionsAsync(entity.VenueId, cancellationToken);

        entity.SetPrices(
            request.Prices.Select(p => (p.SectionId, p.Price)).ToList(),
            sections.Select(s => s.Id).ToHashSet());

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var sectionNames = sections.ToDictionary(s => s.Id, s => s.Name);
        return new SetEventPricesResponse
        {
            EventId = entity.Id,
            Prices = entity.PriceCategories
                .Select(p => new SetEventPriceItemResponse
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
