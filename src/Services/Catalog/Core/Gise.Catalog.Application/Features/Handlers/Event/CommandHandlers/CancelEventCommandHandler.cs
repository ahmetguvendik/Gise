using Gise.Catalog.Application.Features.Commands.Event.Request;
using Gise.Catalog.Application.Features.Commands.Event.Response;
using Gise.Catalog.Application.Interfaces;
using Gise.Catalog.Application.UnitOfWorks;
using Gise.Catalog.Domain.Enums;
using Gise.Contracts.Events;
using Gise.Exceptions;
using MediatR;

namespace Gise.Catalog.Application.Features.Handlers.Event.CommandHandlers;

public sealed class CancelEventCommandHandler : IRequestHandler<CancelEventCommand, CancelEventResponse>
{
    private readonly IEventRepository _eventRepository;
    private readonly IOutboxMessageRepository _outboxMessageRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public CancelEventCommandHandler(
        IEventRepository eventRepository,
        IOutboxMessageRepository outboxMessageRepository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _eventRepository = eventRepository;
        _outboxMessageRepository = outboxMessageRepository;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<CancelEventResponse> Handle(CancelEventCommand request, CancellationToken cancellationToken)
    {
        var entity = await _eventRepository.GetForUpdateAsync(request.EventId, cancellationToken)
            ?? throw NotFoundException.Event(request.EventId);

        if (entity.Status is EventStatus.Draft or EventStatus.PendingReview)
            throw BusinessException.DraftCannotBeCancelled(entity.Id);

        if (entity.Status == EventStatus.Cancelled)
            throw BusinessException.EventCancelled(entity.Id);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        if (entity.StartsAt <= now)
            throw BusinessException.EventStarted(entity.Id);

        entity.Status = EventStatus.Cancelled;
        entity.CancelledAt = now;
        entity.CancellationReason = request.Reason.Trim();

        // Durumla aynı SaveChanges'te: Reservation satışı durduracak, Order ve Payment iade edecek.
        await _outboxMessageRepository.AddCatalogEventAsync(entity.Id, new EventCancelled
        {
            EventId = entity.Id,
            Reason = entity.CancellationReason,
            CancelledAt = now
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CancelEventResponse
        {
            Id = entity.Id,
            Status = entity.Status.ToString(),
            CancelledAt = now,
            CancellationReason = entity.CancellationReason
        };
    }
}
