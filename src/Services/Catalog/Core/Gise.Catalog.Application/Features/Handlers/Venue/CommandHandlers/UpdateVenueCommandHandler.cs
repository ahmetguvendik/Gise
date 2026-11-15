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
        await _cache.RemoveAsync(CacheKeys.Venue(request.Id), cancellationToken);
        await _cache.RemoveAsync(CacheKeys.Venues, cancellationToken);

        // Mekânın adı ve şehri etkinlik özetine de girer; mekândaki etkinliklerin önbelleği eski adla kalmasın.
        foreach (var eventId in await _eventRepository.GetIdsByVenueAsync(request.Id, cancellationToken))
        {
            await _cache.RemoveAsync(CacheKeys.Event(eventId), cancellationToken);
            await _cache.RemoveAsync(CacheKeys.EventSummary(eventId), cancellationToken);
        }

        return new UpdateVenueResponse { Id = venue.Id, Name = venue.Name, City = venue.City };
    }
}
