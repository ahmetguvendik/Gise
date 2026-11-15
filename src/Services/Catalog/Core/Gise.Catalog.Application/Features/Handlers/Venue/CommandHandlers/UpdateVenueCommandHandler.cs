using Gise.Catalog.Application.Caching;
using Gise.Catalog.Application.Features.Commands.Venue.Request;
using Gise.Catalog.Application.Features.Commands.Venue.Response;
using Gise.Catalog.Application.Interfaces;
using Gise.Catalog.Application.UnitOfWorks;
using Gise.Exceptions;
using MediatR;

namespace Gise.Catalog.Application.Features.Handlers.Venue.CommandHandlers;

public sealed class UpdateVenueCommandHandler : IRequestHandler<UpdateVenueCommand, UpdateVenueResponse>
{
    private readonly IVenueRepository _venueRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;

    public UpdateVenueCommandHandler(IVenueRepository venueRepository, IEventRepository eventRepository, IUnitOfWork unitOfWork, ICacheService cache)
    {
        _venueRepository = venueRepository;
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<UpdateVenueResponse> Handle(UpdateVenueCommand request, CancellationToken cancellationToken)
    {
        var venue = await _venueRepository.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.Venue(request.Id);

        venue.Name = request.Name.Trim();
        venue.City = request.City.Trim();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Değişen veri önbellekte eski kalmasın (Telgraf gibi: kaydettikten sonra anahtar silinir).
        // Mekânın adı ve şehri etkinlik özetine de girer; mekândaki etkinliklerin önbelleği eski adla kalmasın. Hepsi tek çağrıda.
        var keys = CacheKeys.ForVenue(request.Id).ToList();
        foreach (var eventId in await _eventRepository.GetIdsByVenueAsync(request.Id, cancellationToken))
            keys.AddRange(CacheKeys.ForEvent(eventId));
        await _cache.RemoveAsync(keys, cancellationToken);

        return new UpdateVenueResponse { Id = venue.Id, Name = venue.Name, City = venue.City };
    }
}
