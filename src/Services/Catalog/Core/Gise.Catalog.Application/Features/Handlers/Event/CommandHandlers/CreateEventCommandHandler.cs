using Gise.Catalog.Application.Exceptions;
using Gise.Catalog.Application.Features.Commands.Event.Request;
using Gise.Catalog.Application.Features.Commands.Event.Response;
using Gise.Catalog.Application.Interfaces;
using Gise.Catalog.Application.UnitOfWorks;

namespace Gise.Catalog.Application.Features.Handlers.Event.CommandHandlers;

public sealed class CreateEventCommandHandler
{
    private readonly IEventRepository _eventRepository;
    private readonly IVenueRepository _venueRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public CreateEventCommandHandler(
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

    public async Task<CreateEventResponse> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        if (!await _venueRepository.ExistsAsync(request.VenueId, cancellationToken))
            throw new NotFoundException("Mekân bulunamadı.");

        var entity = Domain.Entities.Event.Create(
            request.Title,
            request.VenueId,
            request.StartsAt.UtcDateTime,
            request.OrganizerId,
            _timeProvider.GetUtcNow().UtcDateTime);

        await _eventRepository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
