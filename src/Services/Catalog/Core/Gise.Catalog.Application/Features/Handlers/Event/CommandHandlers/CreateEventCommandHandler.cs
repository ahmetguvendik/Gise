using Gise.Catalog.Application.Caching;
using Gise.Catalog.Application.Features.Commands.Event.Request;
using Gise.Catalog.Application.Features.Commands.Event.Response;
using Gise.Catalog.Application.Interfaces;
using Gise.Catalog.Application.UnitOfWorks;
using Gise.Catalog.Domain.Enums;
using Gise.Exceptions;
using MediatR;

namespace Gise.Catalog.Application.Features.Handlers.Event.CommandHandlers;

public sealed class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, CreateEventResponse>
{
    private readonly IEventRepository _eventRepository;
    private readonly IVenueRepository _venueRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;

    public CreateEventCommandHandler(
        IEventRepository eventRepository,
        IVenueRepository venueRepository,
        IUnitOfWork unitOfWork, ICacheService cache)
    {
        _eventRepository = eventRepository;
        _venueRepository = venueRepository;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<CreateEventResponse> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        if (!await _venueRepository.ExistsAsync(request.VenueId, cancellationToken))
            throw NotFoundException.Venue(request.VenueId);

        // Etkinlik taslak olarak açılır; fiyatlar girildikten sonra yayınlanır.
        var entity = new Domain.Entities.Event
        {
            Title = request.Title.Trim(),
            VenueId = request.VenueId,
            StartsAt = request.StartsAt.UtcDateTime,
            OrganizerId = request.OrganizerId,
            Status = EventStatus.Draft
        };

        await _eventRepository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Değişen veri önbellekte eski kalmasın (Telgraf gibi: kaydettikten sonra anahtar silinir).
        // Mekânın "etkinliği var" bilgisi (HasEvents) değişti: mekân yanıtları yeniden okunsun.
        await _cache.RemoveAsync(CacheKeys.Venue(entity.VenueId), cancellationToken);
        await _cache.RemoveAsync(CacheKeys.Venues, cancellationToken);

        return new CreateEventResponse
        {
            Id = entity.Id,
            Title = entity.Title,
            VenueId = entity.VenueId,
            StartsAt = entity.StartsAt,
            Status = entity.Status.ToString(),
            CreatedAt = entity.CreatedAt
        };
    }
}
