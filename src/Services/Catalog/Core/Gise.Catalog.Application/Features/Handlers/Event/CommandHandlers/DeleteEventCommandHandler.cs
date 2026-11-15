using Gise.Catalog.Application.Caching;
using Gise.Catalog.Application.Features.Commands.Event.Request;
using Gise.Catalog.Application.Interfaces;
using Gise.Catalog.Application.UnitOfWorks;
using Gise.Catalog.Domain.Enums;
using Gise.Exceptions;
using MediatR;

namespace Gise.Catalog.Application.Features.Handlers.Event.CommandHandlers;

public sealed class DeleteEventCommandHandler : IRequestHandler<DeleteEventCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;

    public DeleteEventCommandHandler(IEventRepository eventRepository, IUnitOfWork unitOfWork, ICacheService cache)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task Handle(DeleteEventCommand request, CancellationToken cancellationToken)
    {
        var entity = await _eventRepository.GetForUpdateAsync(request.EventId, cancellationToken)
            ?? throw NotFoundException.Event(request.EventId);

        if (!request.IsAdmin && entity.OrganizerId != request.UserId)
            throw ForbiddenException.NotEventOwner(entity.Id);

        if (entity.Status == EventStatus.PendingReview)
            throw BusinessException.EventInReview(entity.Id);

        // Yayındaki etkinliğin satış ve bilet geçmişi kaybolmamalı; o iptal edilir.
        if (entity.Status != EventStatus.Draft)
            throw BusinessException.OnlyDraftCanBeDeleted(entity.Id);

        // Fiyatlar zincirleme silinir. Taslağı başka servis bilmediği için olay gönderilmez.
        _eventRepository.Remove(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Değişen veri önbellekte eski kalmasın (Telgraf gibi: kaydettikten sonra anahtar silinir).
        await _cache.RemoveAsync(CacheKeys.Event(request.EventId), cancellationToken);
        await _cache.RemoveAsync(CacheKeys.EventSummary(request.EventId), cancellationToken);

        // Mekânın "etkinliği var" bilgisi (HasEvents) değişti: mekân yanıtları yeniden okunsun.
        await _cache.RemoveAsync(CacheKeys.Venue(entity.VenueId), cancellationToken);
        await _cache.RemoveAsync(CacheKeys.Venues, cancellationToken);
    }
}
