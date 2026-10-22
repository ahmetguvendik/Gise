using Gise.Catalog.Application.Features.Commands.Event.Request;
using Gise.Catalog.Application.Features.Commands.Event.Response;
using Gise.Catalog.Application.Interfaces;
using Gise.Catalog.Application.UnitOfWorks;
using Gise.Catalog.Domain.Enums;
using Gise.Contracts.Events;
using Gise.Exceptions;
using MediatR;

namespace Gise.Catalog.Application.Features.Handlers.Event.CommandHandlers;

public sealed class UpdateEventCommandHandler : IRequestHandler<UpdateEventCommand, UpdateEventResponse>
{
    private readonly IEventRepository _eventRepository;
    private readonly IVenueRepository _venueRepository;
    private readonly IOutboxMessageRepository _outboxMessageRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public UpdateEventCommandHandler(
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

    public async Task<UpdateEventResponse> Handle(UpdateEventCommand request, CancellationToken cancellationToken)
    {
        var entity = await _eventRepository.GetForUpdateAsync(request.EventId, cancellationToken)
            ?? throw NotFoundException.Event(request.EventId);

        // Organizatör yalnızca kendi etkinliğini değiştirir; admin hepsini.
        if (!request.IsAdmin && entity.OrganizerId != request.UserId)
            throw ForbiddenException.NotEventOwner(entity.Id);

        if (entity.Status == EventStatus.Cancelled)
            throw BusinessException.EventCancelled(entity.Id);

        if (entity.StartsAt <= _timeProvider.GetUtcNow().UtcDateTime)
            throw BusinessException.EventStarted(entity.Id);

        if (request.VenueId != entity.VenueId)
        {
            // Reservation koltuk stoğunu mekânın koltuklarıyla kurar: yayından sonra mekân değişmez.
            if (entity.Status == EventStatus.Published)
                throw BusinessException.VenueLocked(entity.Id);

            if (!await _venueRepository.ExistsAsync(request.VenueId, cancellationToken))
                throw NotFoundException.Venue(request.VenueId);

            // Fiyatlar eski mekânın bölümlerine bağlıydı; yeni mekân için yeniden girilir.
            entity.PriceCategories.Clear();
            entity.VenueId = request.VenueId;
        }

        var previousTitle = entity.Title;
        var previousStartsAt = entity.StartsAt;
        entity.Title = request.Title.Trim();
        entity.StartsAt = request.StartsAt.UtcDateTime;

        // Taslağı başka servis bilmiyor; yalnızca yayındaki etkinliğin değişikliği duyurulur.
        if (entity.Status == EventStatus.Published && (entity.Title != previousTitle || entity.StartsAt != previousStartsAt))
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
                PreviousStartsAt = previousStartsAt
            }, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateEventResponse
        {
            Id = entity.Id,
            Title = entity.Title,
            VenueId = entity.VenueId,
            StartsAt = entity.StartsAt,
            Status = entity.Status.ToString()
        };
    }
}
