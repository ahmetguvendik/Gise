using Gise.Catalog.Application.Caching;
using Gise.Catalog.Application.Features.Commands.Venue.Request;
using Gise.Catalog.Application.Features.Commands.Venue.Response;
using Gise.Catalog.Application.Interfaces;
using Gise.Catalog.Application.UnitOfWorks;
using Gise.Exceptions;
using MediatR;

namespace Gise.Catalog.Application.Features.Handlers.Venue.CommandHandlers;

public sealed class UpdateVenueLayoutCommandHandler : IRequestHandler<UpdateVenueLayoutCommand, UpdateVenueLayoutResponse>
{
    private readonly IVenueRepository _venueRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;

    public UpdateVenueLayoutCommandHandler(IVenueRepository venueRepository, IEventRepository eventRepository, IUnitOfWork unitOfWork, ICacheService cache)
    {
        _venueRepository = venueRepository;
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<UpdateVenueLayoutResponse> Handle(UpdateVenueLayoutCommand request, CancellationToken cancellationToken)
    {
        var venue = await _venueRepository.GetWithLayoutForUpdateAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.Venue(request.Id);

        // Taslak etkinliğin fiyatları bölümlere bağlı, yayındakinin koltukları satılıyor: etkinlik varken düzen değişmez.
        if (await _eventRepository.AnyForVenueAsync(venue.Id, cancellationToken))
            throw BusinessException.VenueHasEvents(venue.Id);

        // Düzen bütün olarak yenilenir: eski bölümler ve koltuklar silinir, yenileri sıra tanımından üretilir.
        venue.Sections.Clear();
        foreach (var section in request.Sections.ToSections())
            venue.Sections.Add(section);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Değişen veri önbellekte eski kalmasın (Telgraf gibi: kaydettikten sonra anahtar silinir).
        await _cache.RemoveAsync(CacheKeys.Venue(request.Id), cancellationToken);
        await _cache.RemoveAsync(CacheKeys.Venues, cancellationToken);

        return new UpdateVenueLayoutResponse
        {
            Id = venue.Id,
            SectionCount = venue.Sections.Count,
            SeatCount = venue.Sections.Sum(s => s.Seats.Count)
        };
    }
}
