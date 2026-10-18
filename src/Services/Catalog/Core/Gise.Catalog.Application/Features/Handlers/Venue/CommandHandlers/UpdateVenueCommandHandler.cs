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
    private readonly IUnitOfWork _unitOfWork;

    public UpdateVenueCommandHandler(IVenueRepository venueRepository, IUnitOfWork unitOfWork)
    {
        _venueRepository = venueRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdateVenueResponse> Handle(UpdateVenueCommand request, CancellationToken cancellationToken)
    {
        var venue = await _venueRepository.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.Venue(request.Id);

        venue.Name = request.Name.Trim();
        venue.City = request.City.Trim();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateVenueResponse { Id = venue.Id, Name = venue.Name, City = venue.City };
    }
}
