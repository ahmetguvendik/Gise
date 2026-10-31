using Gise.Reservation.Application.Features.Commands.SeatHold.Request;
using Gise.Reservation.Application.Features.Commands.SeatHold.Response;
using Gise.Reservation.Application.Interfaces;
using Gise.Reservation.Application.UnitOfWorks;
using Gise.Reservation.Domain.Enums;
using Gise.Contracts.Realtime;
using Gise.Exceptions;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace Gise.Reservation.Application.Features.Handlers.SeatHold.CommandHandlers;

public sealed class HoldSeatsCommandHandler : IRequestHandler<HoldSeatsCommand, HoldSeatsResponse>
{
    private readonly IEventStockRepository _eventStockRepository;
    private readonly ISeatHoldRepository _seatHoldRepository;
    private readonly ISeatLockService _seatLockService;
    private readonly ISeatUpdatePublisher _seatUpdatePublisher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _holdDuration;

    public HoldSeatsCommandHandler(
        IEventStockRepository eventStockRepository,
        ISeatHoldRepository seatHoldRepository,
        ISeatLockService seatLockService,
        ISeatUpdatePublisher seatUpdatePublisher,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        IConfiguration configuration)
    {
        _seatUpdatePublisher = seatUpdatePublisher;
        _eventStockRepository = eventStockRepository;
        _seatHoldRepository = seatHoldRepository;
        _seatLockService = seatLockService;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
        _holdDuration = TimeSpan.FromMinutes(configuration.GetRequiredSection("ReservationRules").GetValue<int>("HoldMinutes"));
    }

    public async Task<HoldSeatsResponse> Handle(HoldSeatsCommand request, CancellationToken cancellationToken)
    {
        // Aynı sipariş için komut ikinci kez gelirse (yeniden teslim) var olan tutma döner. Sipariş bu arada kapandıysa
        // (ReleaseSeats bu komuttan önce işlendiyse) koltuklar tutulmaz.
        var existing = await _seatHoldRepository.GetByOrderIdAsync(request.OrderId, cancellationToken);
        if (existing is not null)
        {
            if (existing.Status != HoldStatus.Active)
                throw BusinessException.HoldOrderClosed(request.OrderId);

            return await ToResponseAsync(existing, cancellationToken);
        }

        var stock = await _eventStockRepository.GetByIdAsync(request.EventId, cancellationToken)
            ?? throw NotFoundException.EventStock(request.EventId);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        if (stock.CancelledAt is not null || stock.StartsAt <= now)
            throw BusinessException.EventNotOnSale(stock.Id);

        var seats = await _eventStockRepository.GetSeatsAsync(stock.Id, request.SeatIds, cancellationToken);
        if (seats.Count != request.SeatIds.Count)
            throw BusinessException.SeatsNotInEvent(request.SeatIds.Except(seats.Select(s => s.SeatId)));

        // Satılmış koltuk kalıcı olarak doludur; Redis'e sormaya gerek yok.
        if (seats.Any(s => s.Status == SeatStatus.Sold))
            throw ConflictException.SeatsUnavailable();

        var hold = new Domain.Entities.SeatHold
        {
            OrderId = request.OrderId,
            EventId = stock.Id,
            CustomerId = request.CustomerId,
            SeatIds = request.SeatIds,
            ExpiresAt = now.Add(_holdDuration)
        };

        // Asıl yarış burada çözülür: aynı koltuğa aynı anda gelen isteklerden yalnızca biri kilidi alır. Kilidin değeri
        // siparişin ID'si: kayıt yazılamadan kalan kilit, komut yeniden gelince aynı siparişin sayılır.
        var result = await _seatLockService.TryLockAsync(stock.Id, request.CustomerId, request.SeatIds, request.OrderId, _holdDuration);
        if (result == SeatLockResult.SeatTaken)
            throw ConflictException.SeatsUnavailable();
        if (result == SeatLockResult.UserAlreadyHolding)
            throw ConflictException.HoldAlreadyActive(stock.Id);

        try
        {
            await _seatHoldRepository.AddAsync(hold, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Kayıt yazılamazsa kilit de kaldırılır; koltuklar süre dolana kadar boşuna kilitli kalmasın.
            await _seatLockService.ReleaseAsync(stock.Id, request.CustomerId, request.SeatIds, request.OrderId);
            throw;
        }

        // Haritaya bakan herkes koltukları "sepette" görür.
        await _seatUpdatePublisher.PublishAsync(stock.Id, request.SeatIds, SeatState.Held);

        return ToResponse(hold, seats);
    }

    private async Task<HoldSeatsResponse> ToResponseAsync(Domain.Entities.SeatHold hold, CancellationToken cancellationToken)
        => ToResponse(hold, await _eventStockRepository.GetSeatsAsync(hold.EventId, hold.SeatIds, cancellationToken));

    private static HoldSeatsResponse ToResponse(Domain.Entities.SeatHold hold, IReadOnlyList<Domain.Entities.EventSeat> seats)
        => new()
        {
            HoldId = hold.Id,
            EventId = hold.EventId,
            ExpiresAt = hold.ExpiresAt,
            TotalPrice = seats.Sum(s => s.Price),
            Seats = seats
                .OrderBy(s => s.SectionName)
                .ThenBy(s => s.Row.Length)
                .ThenBy(s => s.Row)
                .ThenBy(s => s.Number)
                .Select(s => new HeldSeatResponse
                {
                    SeatId = s.SeatId,
                    SectionName = s.SectionName,
                    Row = s.Row,
                    Number = s.Number,
                    Price = s.Price
                })
                .ToList()
        };
}
