using Gise.Catalog.Application.Features.Commands.Venue.Request;
using Gise.Catalog.Application.Features.Commands.Venue.Response;
using Gise.Catalog.Application.Interfaces;
using Gise.Catalog.Application.UnitOfWorks;
using Gise.Catalog.Domain.Entities;
using MediatR;

namespace Gise.Catalog.Application.Features.Handlers.Venue.CommandHandlers;

public sealed class CreateVenueCommandHandler : IRequestHandler<CreateVenueCommand, CreateVenueResponse>
{
    private readonly IVenueRepository _venueRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateVenueCommandHandler(IVenueRepository venueRepository, IUnitOfWork unitOfWork)
    {
        _venueRepository = venueRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateVenueResponse> Handle(CreateVenueCommand request, CancellationToken cancellationToken)
    {
        // Koltuklar sıra tanımından üretilir: her sıradaki koltuklar 1'den başlayarak numaralanır.
        var venue = new Domain.Entities.Venue
        {
            Name = request.Name.Trim(),
            City = request.City.Trim(),
            Sections = request.Sections
                .Select(section => new Section
                {
                    Name = section.Name.Trim(),
                    Seats = section.Rows
                        .SelectMany(row => Enumerable.Range(1, row.SeatCount)
                            .Select(number => new Seat { Row = row.Row.Trim(), Number = number }))
                        .ToList()
                })
                .ToList()
        };

        await _venueRepository.AddAsync(venue, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
