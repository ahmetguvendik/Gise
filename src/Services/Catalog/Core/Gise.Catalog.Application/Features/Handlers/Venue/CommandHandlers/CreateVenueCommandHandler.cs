using Gise.Catalog.Application.Features.Commands.Venue.Request;
using Gise.Catalog.Application.Features.Commands.Venue.Response;
using Gise.Catalog.Application.Interfaces;
using Gise.Catalog.Application.UnitOfWorks;
using Mediator;

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

    public async ValueTask<CreateVenueResponse> Handle(CreateVenueCommand request, CancellationToken cancellationToken)
    {
        var venue = Domain.Entities.Venue.Create(request.Name, request.City);

        foreach (var sectionRequest in request.Sections)
        {
            var section = venue.AddSection(sectionRequest.Name);
            foreach (var row in sectionRequest.Rows)
                section.AddRow(row.Row, row.SeatCount);
        }

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
