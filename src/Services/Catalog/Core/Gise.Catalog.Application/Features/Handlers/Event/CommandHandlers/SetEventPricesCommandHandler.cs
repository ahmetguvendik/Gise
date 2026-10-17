using Gise.Catalog.Application.Features.Commands.Event.Request;
using Gise.Catalog.Application.Features.Commands.Event.Response;
using Gise.Catalog.Application.Interfaces;
using Gise.Catalog.Application.UnitOfWorks;
using Gise.Catalog.Domain.Entities;
using Gise.Catalog.Domain.Enums;
using Gise.Exceptions;
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
            ?? throw NotFoundException.Event(request.EventId);

        // Organizatör yalnızca kendi etkinliğinin fiyatlarını belirler; admin hepsini.
        if (!request.IsAdmin && entity.OrganizerId != request.UserId)
            throw ForbiddenException.NotEventOwner(entity.Id);

        // Yayındaki etkinliğin fiyatı da değişebilir; satılmış biletler sipariş anındaki fiyatı korur.
        if (entity.Status == EventStatus.Cancelled)
            throw BusinessException.EventCancelled(entity.Id);

        var sections = await _venueRepository.GetSectionsAsync(entity.VenueId, cancellationToken);
        var sectionNames = sections.ToDictionary(s => s.Id, s => s.Name);

        var foreignSection = request.Prices.FirstOrDefault(p => !sectionNames.ContainsKey(p.SectionId));
        if (foreignSection is not null)
            throw BusinessException.SectionNotInVenue(foreignSection.SectionId);

        // Liste bütün olarak uygulanır: listede olmayan bölümün fiyatı kalkar, olanınki güncellenir ya da eklenir.
        // Aynı bölümün iki kez gelmediğini validator garanti eder.
        var requested = request.Prices.ToDictionary(p => p.SectionId, p => p.Price);

        foreach (var removed in entity.PriceCategories.Where(p => !requested.ContainsKey(p.SectionId)).ToList())
            entity.PriceCategories.Remove(removed);

        foreach (var (sectionId, price) in requested)
        {
            var existing = entity.PriceCategories.FirstOrDefault(p => p.SectionId == sectionId);
            if (existing is null)
                entity.PriceCategories.Add(new PriceCategory { EventId = entity.Id, SectionId = sectionId, Price = price });
            else
                existing.Price = price;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
