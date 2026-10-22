using Gise.Catalog.Application.Features.Commands.Event.Request;
using Gise.Catalog.Application.Interfaces;
using Gise.Catalog.Application.UnitOfWorks;
using Gise.Catalog.Domain.Enums;
using Gise.Exceptions;
using MediatR;

namespace Gise.Catalog.Application.Features.Handlers.Event.CommandHandlers;

public sealed class WithdrawEventCommandHandler : IRequestHandler<WithdrawEventCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;

    public WithdrawEventCommandHandler(IEventRepository eventRepository, IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(WithdrawEventCommand request, CancellationToken cancellationToken)
    {
        var entity = await _eventRepository.GetForUpdateAsync(request.EventId, cancellationToken)
            ?? throw NotFoundException.Event(request.EventId);

        if (!request.IsAdmin && entity.OrganizerId != request.UserId)
            throw ForbiddenException.NotEventOwner(entity.Id);

        if (entity.Status != EventStatus.PendingReview)
            throw BusinessException.EventNotInReview(entity.Id);

        entity.Status = EventStatus.Draft;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
