using Gise.Catalog.Application.Features.Commands.Event.Request;
using Gise.Catalog.Application.Interfaces;
using Gise.Catalog.Application.UnitOfWorks;
using Gise.Catalog.Domain.Enums;
using Gise.Exceptions;
using MediatR;

namespace Gise.Catalog.Application.Features.Handlers.Event.CommandHandlers;

public sealed class SubmitEventCommandHandler : IRequestHandler<SubmitEventCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IVenueRepository _venueRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public SubmitEventCommandHandler(
        IEventRepository eventRepository,
        IVenueRepository venueRepository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _eventRepository = eventRepository;
        _venueRepository = venueRepository;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task Handle(SubmitEventCommand request, CancellationToken cancellationToken)
    {
        var entity = await _eventRepository.GetForUpdateAsync(request.EventId, cancellationToken)
            ?? throw NotFoundException.Event(request.EventId);

        if (!request.IsAdmin && entity.OrganizerId != request.UserId)
            throw ForbiddenException.NotEventOwner(entity.Id);

        if (entity.Status == EventStatus.PendingReview)
            throw BusinessException.EventInReview(entity.Id);

        if (entity.Status != EventStatus.Draft)
            throw BusinessException.EventNotDraft(entity.Id);

        if (entity.StartsAt <= _timeProvider.GetUtcNow().UtcDateTime)
            throw BusinessException.EventAlreadyStarted(entity.Id);

        // Admin yarım taslakla uğraşmasın: yayın için gereken her şey gönderirken de aranır.
        var sections = await _venueRepository.GetSectionsAsync(entity.VenueId, cancellationToken);
        var unpriced = sections
            .Where(s => entity.PriceCategories.All(p => p.SectionId != s.Id))
            .Select(s => s.Name)
            .ToList();
        if (unpriced.Count > 0)
            throw BusinessException.MissingSectionPrices(unpriced);

        // Taslağı başka servis bilmiyor; olay gönderilmez.
        entity.Status = EventStatus.PendingReview;
        entity.ReturnReason = null;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
