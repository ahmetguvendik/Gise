using Gise.Catalog.Application.Caching;
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
    private readonly ICacheService _cache;

    public DeleteVenueCommandHandler(IVenueRepository venueRepository, IEventRepository eventRepository, IUnitOfWork unitOfWork, ICacheService cache)
    {
        _venueRepository = venueRepository;
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _cache = cache;
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

        // Değişen veri önbellekte eski kalmasın (Telgraf gibi: kaydettikten sonra anahtar silinir).
        await _cache.RemoveAsync(CacheKeys.ForVenue(request.Id), cancellationToken);
    }
}
