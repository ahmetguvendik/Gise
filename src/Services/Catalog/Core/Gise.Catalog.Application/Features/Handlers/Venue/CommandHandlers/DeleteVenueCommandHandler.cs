using Gise.Catalog.Application.Features.Commands.Venue.Request;
using Gise.Catalog.Application.Interfaces;
using Gise.Catalog.Application.UnitOfWorks;
using Gise.Exceptions;
using MediatR;

namespace Gise.Catalog.Application.Features.Handlers.Venue.CommandHandlers;

public sealed class DeleteVenueCommandHandler : IRequestHandler<DeleteVenueCommand>
{
    private readonly IVenueRepository _venueRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteVenueCommandHandler(IVenueRepository venueRepository, IEventRepository eventRepository, IUnitOfWork unitOfWork)
    {
        _venueRepository = venueRepository;
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteVenueCommand request, CancellationToken cancellationToken)
    {
        var venue = await _venueRepository.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.Venue(request.Id);

        if (await _eventRepository.AnyForVenueAsync(venue.Id, cancellationToken))
            throw BusinessException.VenueHasEvents(venue.Id);

        // Bölümler ve koltuklar veritabanında zincirleme silinir.
        _venueRepository.Remove(venue);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
