using Gise.Catalog.Application.Caching;
using Gise.Catalog.Application.Features.Commands.Event.Request;
using Gise.Catalog.Application.Interfaces;
using Gise.Catalog.Application.UnitOfWorks;
using Gise.Catalog.Domain.Enums;
using Gise.Exceptions;
using MediatR;

namespace Gise.Catalog.Application.Features.Handlers.Event.CommandHandlers;

public sealed class ReturnEventCommandHandler : IRequestHandler<ReturnEventCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;

    public ReturnEventCommandHandler(IEventRepository eventRepository, IUnitOfWork unitOfWork, ICacheService cache)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task Handle(ReturnEventCommand request, CancellationToken cancellationToken)
    {
        var entity = await _eventRepository.GetForUpdateAsync(request.EventId, cancellationToken)
            ?? throw NotFoundException.Event(request.EventId);

        if (entity.Status != EventStatus.PendingReview)
            throw BusinessException.EventNotInReview(entity.Id);

        entity.Status = EventStatus.Draft;
        entity.ReturnReason = request.Reason.Trim();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Değişen veri önbellekte eski kalmasın (Telgraf gibi: kaydettikten sonra anahtar silinir).
        await _cache.RemoveAsync(CacheKeys.ForEvent(request.EventId), cancellationToken);
    }
}
