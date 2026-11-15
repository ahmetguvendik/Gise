using Gise.Catalog.Application.Caching;
using Gise.Catalog.Application.Features.Commands.Venue.Request;
using Gise.Catalog.Application.Features.Commands.Venue.Response;
using Gise.Catalog.Application.Interfaces;
using Gise.Catalog.Application.UnitOfWorks;
using MediatR;

namespace Gise.Catalog.Application.Features.Handlers.Venue.CommandHandlers;

public sealed class CreateVenueCommandHandler : IRequestHandler<CreateVenueCommand, CreateVenueResponse>
{
    private readonly IVenueRepository _venueRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;

    public CreateVenueCommandHandler(IVenueRepository venueRepository, IUnitOfWork unitOfWork, ICacheService cache)
    {
        _venueRepository = venueRepository;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<CreateVenueResponse> Handle(CreateVenueCommand request, CancellationToken cancellationToken)
    {
        var venue = new Domain.Entities.Venue
        {
            Name = request.Name.Trim(),
            City = request.City.Trim(),
            Sections = request.Sections.ToSections()
        };

        await _venueRepository.AddAsync(venue, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Değişen veri önbellekte eski kalmasın (Telgraf gibi: kaydettikten sonra anahtar silinir).
        await _cache.RemoveAsync(CacheKeys.Venues, cancellationToken);

        return new CreateVenueResponse
        {
            Id = venue.Id,
            Name = venue.Name,
            City = venue.City,
            SectionCount = venue.Sections.Count,
            SeatCount = venue.Sections.Sum(s => s.Seats.Count)
        };
    }
}
