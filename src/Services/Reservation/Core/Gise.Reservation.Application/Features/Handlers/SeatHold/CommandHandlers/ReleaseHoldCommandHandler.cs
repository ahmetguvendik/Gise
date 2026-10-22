using Gise.Reservation.Application.Features.Commands.SeatHold.Request;
using Gise.Reservation.Application.Interfaces;
using Gise.Reservation.Application.UnitOfWorks;
using Gise.Reservation.Domain.Enums;
using Gise.Exceptions;
using MediatR;

namespace Gise.Reservation.Application.Features.Handlers.SeatHold.CommandHandlers;

public sealed class ReleaseHoldCommandHandler : IRequestHandler<ReleaseHoldCommand>
{
    private readonly ISeatHoldRepository _seatHoldRepository;
    private readonly ISeatLockService _seatLockService;
    private readonly IUnitOfWork _unitOfWork;

    public ReleaseHoldCommandHandler(ISeatHoldRepository seatHoldRepository, ISeatLockService seatLockService, IUnitOfWork unitOfWork)
    {
        _seatHoldRepository = seatHoldRepository;
        _seatLockService = seatLockService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ReleaseHoldCommand request, CancellationToken cancellationToken)
    {
        var hold = await _seatHoldRepository.GetForUpdateAsync(request.HoldId, cancellationToken)
            ?? throw NotFoundException.Hold(request.HoldId);

        if (!request.IsAdmin && hold.UserId != request.UserId)
            throw ForbiddenException.NotHoldOwner(hold.Id);

        // Bırakılmış ya da onaylanmış (satılmış) tutmaya dokunulmaz; tekrar gelen istek zararsızdır.
        if (hold.Status != HoldStatus.Active)
            return;

        // Süresi dolmuşsa kilit zaten kalkmıştır; yalnızca bu tutmaya ait kilitler silinir.
        await _seatLockService.ReleaseAsync(hold.EventId, hold.UserId, hold.SeatIds, hold.Id);

        hold.Status = HoldStatus.Released;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
