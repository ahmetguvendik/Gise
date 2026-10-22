using Gise.Catalog.Application.Features.Commands.Event.Request;
using Gise.Catalog.Application.Features.Commands.Event.Response;
using Gise.Catalog.Application.Interfaces;
using Gise.Catalog.Application.UnitOfWorks;
using Gise.Catalog.Domain.Enums;
using Gise.Exceptions;
using MediatR;

namespace Gise.Catalog.Application.Features.Handlers.Event.CommandHandlers;

public sealed class SetEventPosterCommandHandler : IRequestHandler<SetEventPosterCommand, SetEventPosterResponse>
{
    private readonly IEventRepository _eventRepository;
    private readonly IStorageService _storageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public SetEventPosterCommandHandler(
        IEventRepository eventRepository,
        IStorageService storageService,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _eventRepository = eventRepository;
        _storageService = storageService;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<SetEventPosterResponse> Handle(SetEventPosterCommand request, CancellationToken cancellationToken)
    {
        var entity = await _eventRepository.GetForUpdateAsync(request.EventId, cancellationToken)
            ?? throw NotFoundException.Event(request.EventId);

        // Bilgi ve fiyatla aynı kurallar: taslağı sahibi ve admin, yayındakini yalnızca admin değiştirir.
        if (!request.IsAdmin && entity.OrganizerId != request.UserId)
            throw ForbiddenException.NotEventOwner(entity.Id);

        if (entity.Status == EventStatus.Cancelled)
            throw BusinessException.EventCancelled(entity.Id);

        if (entity.Status == EventStatus.PendingReview)
            throw BusinessException.EventInReview(entity.Id);

        if (entity.Status == EventStatus.Published && !request.IsAdmin)
            throw ForbiddenException.PublishedEventAdminOnly(entity.Id);

        if (entity.StartsAt <= _timeProvider.GetUtcNow().UtcDateTime)
            throw BusinessException.EventStarted(entity.Id);

        entity.PosterKey = string.IsNullOrEmpty(request.PosterKey) ? null : request.PosterKey;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new SetEventPosterResponse
        {
            Id = entity.Id,
            PosterUrl = _storageService.ResolvePublicUrl(entity.PosterKey),
            PosterSrcset = _storageService.ResolveSrcset(entity.PosterKey)
        };
    }
}
