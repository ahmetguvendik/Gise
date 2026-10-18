using System.Diagnostics;
using System.Text.Json;
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

public sealed class PublishEventCommandHandler : IRequestHandler<PublishEventCommand, PublishEventResponse>
{
    private readonly IEventRepository _eventRepository;
    private readonly IVenueRepository _venueRepository;
    private readonly IOutboxMessageRepository _outboxMessageRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public PublishEventCommandHandler(
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

    public async Task<PublishEventResponse> Handle(PublishEventCommand request, CancellationToken cancellationToken)
    {
        var entity = await _eventRepository.GetForUpdateAsync(request.EventId, cancellationToken)
            ?? throw NotFoundException.Event(request.EventId);

        // Organizatör yalnızca kendi etkinliğini yayınlar; admin hepsini.
        if (!request.IsAdmin && entity.OrganizerId != request.UserId)
            throw ForbiddenException.NotEventOwner(entity.Id);

        if (entity.Status == EventStatus.Cancelled)
            throw BusinessException.EventCancelled(entity.Id);

        if (entity.Status == EventStatus.Published)
            throw ConflictException.EventAlreadyPublished(entity.Id);

        if (entity.StartsAt <= _timeProvider.GetUtcNow().UtcDateTime)
            throw BusinessException.EventAlreadyStarted(entity.Id);

        // Fiyatı olmayan bölümün koltukları satılamaz; yayından önce her bölüm fiyatlanmış olmalı.
        var sections = await _venueRepository.GetSectionsAsync(entity.VenueId, cancellationToken);
        var unpriced = sections
            .Where(s => entity.PriceCategories.All(p => p.SectionId != s.Id))
            .Select(s => s.Name)
            .ToList();
        if (unpriced.Count > 0)
            throw BusinessException.MissingSectionPrices(unpriced);

        var venue = await _venueRepository.GetByIdAsync(entity.VenueId, cancellationToken)
            ?? throw NotFoundException.Venue(entity.VenueId);

        entity.Status = EventStatus.Published;

        // Olay durum değişikliğiyle aynı SaveChanges'te yazılır: ya ikisi birden kaydedilir ya hiçbiri.
        // Anahtar etkinlik ID'si: aynı etkinliğin olayları Kafka'da aynı bölüme düşer, sırası korunur.
        await _outboxMessageRepository.AddAsync(new OutboxMessage
        {
            Topic = Kafka.CatalogEventsTopic,
            Type = nameof(EventPublished),
            Key = entity.Id.ToString(),
            Payload = JsonSerializer.Serialize(new EventPublished
            {
                EventId = entity.Id,
                VenueId = entity.VenueId,
                Title = entity.Title,
                StartsAt = entity.StartsAt,
                VenueName = venue.Name,
                City = venue.City,
                MinPrice = entity.PriceCategories.Min(p => p.Price)
            }, JsonSerializerOptions.Web),
            TraceParent = Activity.Current?.Id
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new PublishEventResponse
        {
            Id = entity.Id,
            Title = entity.Title,
            Status = entity.Status.ToString()
        };
    }
}
