using Gise.Catalog.Application.Features.Commands.Event.Request;
using Gise.Catalog.Application.Features.Commands.Event.Response;
using Gise.Catalog.Application.Interfaces;
using Gise.Catalog.Application.UnitOfWorks;

namespace Gise.Catalog.Application.Features.Handlers.Event.CommandHandlers;

public sealed class CreateEventCommandHandler
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public CreateEventCommandHandler(IEventRepository eventRepository, IUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<CreateEventResponse> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        var entity = Domain.Entities.Event.Create(
            request.Title,
            request.Venue,
            request.StartsAt.UtcDateTime,
            request.OrganizerId,
            _timeProvider.GetUtcNow().UtcDateTime);

        await _eventRepository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateEventResponse
        {
            Id = entity.Id,
            Title = entity.Title,
            Venue = entity.Venue,
            StartsAt = entity.StartsAt,
            CreatedAt = entity.CreatedAt
        };
    }
}
