using Gise.Catalog.Application.Features.Commands.Event.Request;
using Gise.Catalog.Application.Features.Commands.Event.Response;
using Gise.Catalog.Application.Interfaces;
using Gise.Catalog.Application.UnitOfWorks;
using Gise.Catalog.Domain.Entities;
using Gise.Catalog.Domain.Enums;
using Gise.Contracts.Events;
using Gise.Exceptions;
using MediatR;

namespace Gise.Catalog.Application.Features.Handlers.Event.CommandHandlers;

public sealed class SetEventPricesCommandHandler : IRequestHandler<SetEventPricesCommand, SetEventPricesResponse>
{
    private readonly IEventRepository _eventRepository;
    private readonly IVenueRepository _venueRepository;
    private readonly IOutboxMessageRepository _outboxMessageRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public SetEventPricesCommandHandler(
        IEventRepository eventRepository,
        IVenueRepository venueRepository,
        IOutboxMessageRepository outboxMessageRepository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _eventRepository = eventRepository;
        _venueRepository = venueRepository;
        _outboxMessageRepository = outboxMessageRepository;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<SetEventPricesResponse> Handle(SetEventPricesCommand request, CancellationToken cancellationToken)
    {
        var entity = await _eventRepository.GetForUpdateAsync(request.EventId, cancellationToken)
            ?? throw NotFoundException.Event(request.EventId);

        // Taslağı sahibi ve admin, yayındakini yalnızca admin değiştirir; onay bekleyen değişmez.
        if (!request.IsAdmin && entity.OrganizerId != request.UserId)
            throw ForbiddenException.NotEventOwner(entity.Id);

        if (entity.Status == EventStatus.Cancelled)
            throw BusinessException.EventCancelled(entity.Id);

        if (entity.Status == EventStatus.PendingReview)
            throw BusinessException.EventInReview(entity.Id);

        if (entity.Status == EventStatus.Published && !request.IsAdmin)
            throw ForbiddenException.PublishedEventAdminOnly(entity.Id);

        // Yayındaki etkinliğin fiyatı admin tarafından değişebilir; satılmış biletler sipariş anındaki fiyatı korur.
        if (entity.StartsAt <= _timeProvider.GetUtcNow().UtcDateTime)
            throw BusinessException.EventStarted(entity.Id);

        var sections = await _venueRepository.GetSectionsAsync(entity.VenueId, cancellationToken);
        var sectionNames = sections.ToDictionary(s => s.Id, s => s.Name);

        var foreignSection = request.Prices.FirstOrDefault(p => !sectionNames.ContainsKey(p.SectionId));
        if (foreignSection is not null)
            throw BusinessException.SectionNotInVenue(foreignSection.SectionId);

        // Liste bütün olarak uygulanır: listede olmayan bölümün fiyatı kalkar, olanınki güncellenir ya da eklenir.
        // Aynı bölümün iki kez gelmediğini validator garanti eder.
        var requested = request.Prices.ToDictionary(p => p.SectionId, p => p.Price);

        // Yayındaki etkinlikte fiyatsız bölüm kalamaz; o bölümün koltukları satılamaz hale gelirdi.
        if (entity.Status == EventStatus.Published)
        {
            var unpriced = sections.Where(s => !requested.ContainsKey(s.Id)).Select(s => s.Name).ToList();
            if (unpriced.Count > 0)
                throw BusinessException.MissingSectionPrices(unpriced);
        }

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

        // Yayındaki etkinlikte en düşük fiyat değişebilir; Search gibi dinleyenler güncel hâli alır.
        if (entity.Status == EventStatus.Published)
        {
            var venue = await _venueRepository.GetByIdAsync(entity.VenueId, cancellationToken)
                ?? throw NotFoundException.Venue(entity.VenueId);

            await _outboxMessageRepository.AddCatalogEventAsync(entity.Id, new EventUpdated
            {
                EventId = entity.Id,
                VenueId = entity.VenueId,
                Title = entity.Title,
                StartsAt = entity.StartsAt,
                VenueName = venue.Name,
                City = venue.City,
                MinPrice = entity.PriceCategories.Min(p => p.Price),
                PreviousStartsAt = entity.StartsAt
            }, cancellationToken);
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
