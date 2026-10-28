using Gise.Contracts.Commands;
using Gise.Order.Application.Interfaces;
using Gise.Order.Application.Sagas.Messages;
using Gise.Order.Domain.Entities;
using Gise.Order.Domain.Enums;
using MassTransit;
using Microsoft.Extensions.Configuration;

namespace Gise.Order.Application.Sagas;

// Satın alma saga'sı (bkz. tasarım 4). Order akışı yönetir; Reservation (ve ileride Payment, Ticket) yalnızca kendisine
// gelen komutu yapıp sonucu bildirir. Müşterinin gördüğü sipariş her adımda saga ile aynı transaction'da güncellenir.
public sealed class OrderStateMachine : MassTransitStateMachine<OrderState>
{
    private static readonly Uri ReserveSeatsQueue = new($"queue:{Queues.ReserveSeats}");
    private static readonly Uri ReleaseSeatsQueue = new($"queue:{Queues.ReleaseSeats}");

    public State SeatsPending { get; private set; } = null!;
    public State PaymentPending { get; private set; } = null!;

    public Event<OrderSubmitted> Submitted { get; private set; } = null!;
    public Event<SeatsReserved> SeatsReserved { get; private set; } = null!;
    public Event<SeatReservationFailed> SeatReservationFailed { get; private set; } = null!;
    public Event<OrderCancellationRequested> CancellationRequested { get; private set; } = null!;

    // Ödeme süresi; Reservation hiç cevap vermezse bekleme sınırı da odur. Quartz ile Order veritabanında saklanır.
    public Schedule<OrderState, OrderExpired> Expiry { get; private set; } = null!;

    public OrderStateMachine(IConfiguration configuration)
    {
        var paymentWindow = TimeSpan.FromMinutes(
            configuration.GetRequiredSection("OrderRules").GetValue<int>("PaymentWindowMinutes"));

        InstanceState(x => x.CurrentState);

        Event(() => Submitted, e => e.CorrelateById(c => c.Message.OrderId));
        // Saga kapandıktan sonra gelen geç cevaplar yok sayılır.
        Event(() => SeatsReserved, e =>
        {
            e.CorrelateById(c => c.Message.OrderId);
            e.OnMissingInstance(m => m.Discard());
        });
        Event(() => SeatReservationFailed, e =>
        {
            e.CorrelateById(c => c.Message.OrderId);
            e.OnMissingInstance(m => m.Discard());
        });
        Event(() => CancellationRequested, e =>
        {
            e.CorrelateById(c => c.Message.OrderId);
            e.OnMissingInstance(m => m.Discard());
        });

        Schedule(() => Expiry, x => x.ExpiryTokenId, s =>
        {
            s.Delay = paymentWindow;
            s.Received = r =>
            {
                r.CorrelateById(c => c.Message.OrderId);
                r.OnMissingInstance(m => m.Discard());
            };
        });

        Initially(
            When(Submitted)
                .Send(ReserveSeatsQueue, c => new ReserveSeats
                {
                    OrderId = c.Message.OrderId,
                    EventId = c.Message.EventId,
                    CustomerId = c.Message.CustomerId,
                    SeatIds = c.Message.SeatIds
                })
                // Reservation hiç cevap vermezse sipariş askıda kalmasın.
                .Schedule(Expiry, c => new OrderExpired { OrderId = c.Saga.CorrelationId })
                .TransitionTo(SeatsPending));

        During(SeatsPending,
            When(SeatsReserved)
                .ThenAsync(MarkAwaitingPaymentAsync)
                // Süre tutmanın bitişine eşitlenir: koltuklar bırakılmadan sipariş düşer.
                .Schedule(Expiry, c => new OrderExpired { OrderId = c.Saga.CorrelationId }, c => c.Message.ExpiresAt)
                .TransitionTo(PaymentPending),
            When(SeatReservationFailed)
                .ThenAsync(c => CloseAsync(c, OrderStatus.Cancelled, c.Message.Reason))
                .Unschedule(Expiry)
                .Finalize(),
            When(Expiry.Received)
                // Cevap gelmedi; koltuklar yine de tutulmuş olabilir, bırakılır.
                .Send(ReleaseSeatsQueue, c => new ReleaseSeats { OrderId = c.Saga.CorrelationId })
                .ThenAsync(c => CloseAsync(c, OrderStatus.Expired, "Koltuklar zamanında tutulamadı."))
                .Finalize(),
            // Vazgeçme yalnızca koltuklar tutulduktan sonra kabul edilir (bkz. CancelOrderCommandHandler).
            Ignore(CancellationRequested));

        During(PaymentPending,
            When(CancellationRequested)
                .Unschedule(Expiry)
                .Send(ReleaseSeatsQueue, c => new ReleaseSeats { OrderId = c.Saga.CorrelationId })
                .ThenAsync(c => CloseAsync(c, OrderStatus.Cancelled, "Siparişten vazgeçildi."))
                .Finalize(),
            When(Expiry.Received)
                .Send(ReleaseSeatsQueue, c => new ReleaseSeats { OrderId = c.Saga.CorrelationId })
                .ThenAsync(c => CloseAsync(c, OrderStatus.Expired, "Ödeme süresi doldu."))
                .Finalize(),
            // Aynı cevap ikinci kez gelirse.
            Ignore(SeatsReserved));

        // Kapanan saga silinir; sipariş (orders) geçmiş olarak kalır.
        SetCompletedWhenFinalized();
    }

    private static async Task MarkAwaitingPaymentAsync(BehaviorContext<OrderState, SeatsReserved> context)
    {
        var order = await GetOrderAsync(context, context.Saga.CorrelationId);
        order.Status = OrderStatus.AwaitingPayment;
        order.ExpiresAt = context.Message.ExpiresAt;
        order.TotalAmount = context.Message.TotalPrice;

        foreach (var seat in context.Message.Seats)
        {
            order.Items.Add(new OrderItem
            {
                OrderId = order.Id,
                SeatId = seat.SeatId,
                SectionName = seat.SectionName,
                Row = seat.Row,
                Number = seat.Number,
                Price = seat.Price
            });
        }
    }

    private static async Task CloseAsync<TMessage>(BehaviorContext<OrderState, TMessage> context, OrderStatus status, string reason)
        where TMessage : class
    {
        var order = await GetOrderAsync(context, context.Saga.CorrelationId);
        order.Status = status;
        order.FailureReason = reason;
    }

    // Sipariş, saga ile aynı DbContext'te güncellenir; ikisi birlikte kaydedilir.
    private static async Task<Domain.Entities.Order> GetOrderAsync(ConsumeContext context, Guid orderId)
        => await context.GetServiceOrCreateInstance<IOrderRepository>().GetForUpdateAsync(orderId, context.CancellationToken)
            ?? throw new InvalidOperationException($"'{orderId}' ID'li sipariş bulunamadı.");
}
