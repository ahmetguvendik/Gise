using Gise.Contracts.Realtime;
using Gise.Reservation.Application.Features.Commands.SeatHold.Request;
using Gise.Reservation.Application.Interfaces;
using Gise.Reservation.Application.UnitOfWorks;
using Gise.Reservation.Domain.Enums;
using MediatR;

namespace Gise.Reservation.Application.Features.Handlers.SeatHold.CommandHandlers;

public sealed class ReleaseHoldCommandHandler : IRequestHandler<ReleaseHoldCommand>
{
    private readonly IEventStockRepository _eventStockRepository;
    private readonly ISeatHoldRepository _seatHoldRepository;
    private readonly ISeatLockService _seatLockService;
    private readonly ISeatUpdatePublisher _seatUpdatePublisher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public ReleaseHoldCommandHandler(
        IEventStockRepository eventStockRepository,
        ISeatHoldRepository seatHoldRepository,
        ISeatLockService seatLockService,
        ISeatUpdatePublisher seatUpdatePublisher,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _seatUpdatePublisher = seatUpdatePublisher;
        _eventStockRepository = eventStockRepository;
        _seatHoldRepository = seatHoldRepository;
        _seatLockService = seatLockService;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task Handle(ReleaseHoldCommand request, CancellationToken cancellationToken)
    {
        var hold = await _seatHoldRepository.GetByOrderIdForUpdateAsync(request.OrderId, cancellationToken);
        if (hold is null)
        {
            // Tutma yok: ya koltuklar hiç tutulamadı ya da ReserveSeats henüz işlenmedi (komutlar ayrı kuyruklarda,
            // sıraları garanti değil). Sipariş için "bırakıldı" kaydı yazılır; sonradan gelen ReserveSeats koltuk tutmaz.
            await _seatHoldRepository.AddAsync(new Domain.Entities.SeatHold
            {
                OrderId = request.OrderId,
                EventId = request.EventId,
                CustomerId = request.CustomerId,
                ExpiresAt = _timeProvider.GetUtcNow().UtcDateTime,
                Status = HoldStatus.Released
            }, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        if (hold.Status == HoldStatus.Released)
            return;

        if (hold.Status == HoldStatus.Confirmed)
        {
            // Koltuklar satılmıştı ama bilet çıkarılamadı; para iade ediliyor, koltuklar yeniden satışa açılır
            // (bkz. tasarım 4.5). Redis kilitleri satışta zaten kaldırılmıştı.
            var seats = await _eventStockRepository.GetSeatsForUpdateAsync(hold.EventId, hold.SeatIds, cancellationToken);
            var reopened = seats.Where(s => s.HoldId == hold.Id).ToList();
            foreach (var seat in reopened)
            {
                seat.Status = SeatStatus.Available;
                seat.HoldId = null;
            }

            hold.Status = HoldStatus.Released;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _seatUpdatePublisher.PublishAsync(hold.EventId, reopened.Select(s => s.SeatId).ToList(), SeatState.Available);
            return;
        }

        // Süresi dolmuşsa kilit zaten kalkmıştır; yalnızca bu tutmaya ait kilitler silinir.
        await _seatLockService.ReleaseAsync(hold.EventId, hold.CustomerId, hold.SeatIds, hold.OrderId);

        hold.Status = HoldStatus.Released;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Koltuklar haritada yeniden boş görünür.
        await _seatUpdatePublisher.PublishAsync(hold.EventId, hold.SeatIds, SeatState.Available);
    }
}
