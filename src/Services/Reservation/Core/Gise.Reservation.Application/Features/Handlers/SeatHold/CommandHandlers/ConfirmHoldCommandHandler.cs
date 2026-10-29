using Gise.Exceptions;
using Gise.Reservation.Application.Features.Commands.SeatHold.Request;
using Gise.Reservation.Application.Interfaces;
using Gise.Reservation.Application.UnitOfWorks;
using Gise.Reservation.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace Gise.Reservation.Application.Features.Handlers.SeatHold.CommandHandlers;

public sealed class ConfirmHoldCommandHandler : IRequestHandler<ConfirmHoldCommand>
{
    private readonly IEventStockRepository _eventStockRepository;
    private readonly ISeatHoldRepository _seatHoldRepository;
    private readonly ISeatLockService _seatLockService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeSpan _holdDuration;

    public ConfirmHoldCommandHandler(
        IEventStockRepository eventStockRepository,
        ISeatHoldRepository seatHoldRepository,
        ISeatLockService seatLockService,
        IUnitOfWork unitOfWork,
        IConfiguration configuration)
    {
        _eventStockRepository = eventStockRepository;
        _seatHoldRepository = seatHoldRepository;
        _seatLockService = seatLockService;
        _unitOfWork = unitOfWork;
        _holdDuration = TimeSpan.FromMinutes(configuration.GetRequiredSection("ReservationRules").GetValue<int>("HoldMinutes"));
    }

    public async Task Handle(ConfirmHoldCommand request, CancellationToken cancellationToken)
    {
        var hold = await _seatHoldRepository.GetByOrderIdForUpdateAsync(request.OrderId, cancellationToken);

        // Aynı komut ikinci kez gelirse koltuklar zaten satılmıştır.
        if (hold?.Status == HoldStatus.Confirmed)
            return;

        // Tutma bırakıldıysa (süre doldu, etkinlik iptal edildi) satış yapılmaz; sipariş iptal olur, ödeme iade edilir.
        if (hold is null || hold.Status != HoldStatus.Active)
            throw BusinessException.HoldNotActive(request.OrderId);

        var seats = await _eventStockRepository.GetSeatsForUpdateAsync(hold.EventId, hold.SeatIds, cancellationToken);
        if (seats.Count != hold.SeatIds.Count || seats.Any(s => s.Status == SeatStatus.Sold))
            throw ConflictException.SeatsUnavailable();

        // Ödeme süre dolmadan alınmış ama komut süre dolduktan sonra gelmiş olabilir: kilit bu siparişteyse ya da kimse
        // almadıysa satılır. Koltuğu bu arada başka bir sipariş tuttuysa o kazanır.
        if (!await _seatLockService.TryClaimAsync(hold.EventId, hold.SeatIds, hold.OrderId, _holdDuration))
            throw ConflictException.SeatsUnavailable();

        foreach (var seat in seats)
        {
            seat.Status = SeatStatus.Sold;
            seat.HoldId = hold.Id;
        }
        hold.Status = HoldStatus.Confirmed;

        // Koltuğun sürümü (xmin): aynı koltuğu aynı anda satan ikinci işlem çakışma alır; yeniden denenince koltuğu dolu görür.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Koltuğun kalıcı durumu artık veritabanında. Kilitler kaldırılır; müşteri aynı etkinlikte yeniden sipariş açabilir.
        await _seatLockService.ReleaseAsync(hold.EventId, hold.CustomerId, hold.SeatIds, hold.OrderId);
    }
}
