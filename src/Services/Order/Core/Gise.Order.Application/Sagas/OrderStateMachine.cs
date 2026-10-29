using Gise.Contracts.Commands;
using Gise.Order.Application.Interfaces;
using Gise.Order.Application.Sagas.Messages;
using Gise.Order.Domain.Entities;
using Gise.Order.Domain.Enums;
using MassTransit;
using Microsoft.Extensions.Configuration;

namespace Gise.Order.Application.Sagas;

// Satın alma saga'sı (bkz. tasarım 4). Order akışı yönetir; Reservation, Payment ve Ticket yalnızca kendisine gelen
// komutu yapıp sonucu bildirir. Müşterinin gördüğü sipariş her adımda saga ile aynı transaction'da güncellenir.
public sealed class OrderStateMachine : MassTransitStateMachine<OrderState>
{
    private static readonly Uri ReserveSeatsQueue = new($"queue:{Queues.ReserveSeats}");
    private static readonly Uri ReleaseSeatsQueue = new($"queue:{Queues.ReleaseSeats}");
    private static readonly Uri ConfirmSeatsQueue = new($"queue:{Queues.ConfirmSeats}");
    private static readonly Uri ProcessPaymentQueue = new($"queue:{Queues.ProcessPayment}");
    private static readonly Uri RefundPaymentQueue = new($"queue:{Queues.RefundPayment}");
    private static readonly Uri IssueTicketsQueue = new($"queue:{Queues.IssueTickets}");

    public State SeatsPending { get; private set; } = null!;
    public State PaymentPending { get; private set; } = null!;
    public State PaymentProcessing { get; private set; } = null!;
    public State SeatsConfirming { get; private set; } = null!;
    public State TicketsIssuing { get; private set; } = null!;

    public Event<OrderSubmitted> Submitted { get; private set; } = null!;
    public Event<SeatsReserved> SeatsReserved { get; private set; } = null!;
    public Event<SeatReservationFailed> SeatReservationFailed { get; private set; } = null!;
    public Event<OrderCancellationRequested> CancellationRequested { get; private set; } = null!;
    public Event<OrderPaymentRequested> PaymentRequested { get; private set; } = null!;
    public Event<PaymentSucceeded> PaymentSucceeded { get; private set; } = null!;
    public Event<PaymentFailed> PaymentFailed { get; private set; } = null!;
    public Event<SeatsConfirmed> SeatsConfirmed { get; private set; } = null!;
    public Event<SeatConfirmationFailed> SeatConfirmationFailed { get; private set; } = null!;
    public Event<TicketsIssued> TicketsIssued { get; private set; } = null!;
    public Event<TicketIssueFailed> TicketIssueFailed { get; private set; } = null!;

    // Ödeme süresi; Reservation ya da Payment hiç cevap vermezse bekleme sınırı da odur. Quartz ile Order veritabanında saklanır.
    public Schedule<OrderState, OrderExpired> Expiry { get; private set; } = null!;

    public OrderStateMachine(IConfiguration configuration)
    {
        var paymentWindow = TimeSpan.FromMinutes(
            configuration.GetRequiredSection("OrderRules").GetValue<int>("PaymentWindowMinutes"));

        InstanceState(x => x.CurrentState);

        Event(() => Submitted, e => e.CorrelateById(c => c.Message.OrderId));
        // Saga kapandıktan sonra gelen geç cevaplar ve istekler yok sayılır. Kapanırken ödeme sürüyorsa RefundPayment
        // gönderilmiştir; geç gelen PaymentSucceeded'ın parasını Payment kendisi iade eder.
        Event(() => SeatsReserved, e => { e.CorrelateById(c => c.Message.OrderId); e.OnMissingInstance(m => m.Discard()); });
        Event(() => SeatReservationFailed, e => { e.CorrelateById(c => c.Message.OrderId); e.OnMissingInstance(m => m.Discard()); });
        Event(() => CancellationRequested, e => { e.CorrelateById(c => c.Message.OrderId); e.OnMissingInstance(m => m.Discard()); });
        Event(() => PaymentRequested, e => { e.CorrelateById(c => c.Message.OrderId); e.OnMissingInstance(m => m.Discard()); });
        Event(() => PaymentSucceeded, e => { e.CorrelateById(c => c.Message.OrderId); e.OnMissingInstance(m => m.Discard()); });
        Event(() => PaymentFailed, e => { e.CorrelateById(c => c.Message.OrderId); e.OnMissingInstance(m => m.Discard()); });
        Event(() => SeatsConfirmed, e => { e.CorrelateById(c => c.Message.OrderId); e.OnMissingInstance(m => m.Discard()); });
        Event(() => SeatConfirmationFailed, e => { e.CorrelateById(c => c.Message.OrderId); e.OnMissingInstance(m => m.Discard()); });
        Event(() => TicketsIssued, e => { e.CorrelateById(c => c.Message.OrderId); e.OnMissingInstance(m => m.Discard()); });
        Event(() => TicketIssueFailed, e => { e.CorrelateById(c => c.Message.OrderId); e.OnMissingInstance(m => m.Discard()); });

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
                .Then(c =>
                {
                    c.Saga.EventId = c.Message.EventId;
                    c.Saga.CustomerId = c.Message.CustomerId;
                })
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
                .Then(c => c.Saga.Amount = c.Message.TotalPrice)
                .ThenAsync(MarkAwaitingPaymentAsync)
                // Süre tutmanın bitişine eşitlenir: koltuklar bırakılmadan sipariş düşer.
                .Schedule(Expiry, c => new OrderExpired { OrderId = c.Saga.CorrelationId }, c => c.Message.ExpiresAt)
                .TransitionTo(PaymentPending),
            When(SeatReservationFailed)
                .ThenAsync(c => SetOrderStatusAsync(c, OrderStatus.Cancelled, c.Message.Reason))
                .Unschedule(Expiry)
                .Finalize(),
            When(Expiry.Received)
                // Cevap gelmedi; koltuklar yine de tutulmuş olabilir, bırakılır.
                .Send(ReleaseSeatsQueue, c => ReleaseSeatsFor(c.Saga))
                .ThenAsync(c => SetOrderStatusAsync(c, OrderStatus.Expired, "Koltuklar zamanında tutulamadı."))
                .Finalize(),
            // Vazgeçme yalnızca koltuklar tutulduktan sonra kabul edilir (bkz. CancelOrderCommandHandler).
            Ignore(CancellationRequested));

        During(PaymentPending,
            When(PaymentRequested)
                .Then(c => c.Saga.PaymentId = c.Message.PaymentId)
                .ThenAsync(MarkPaymentProcessingAsync)
                .Send(ProcessPaymentQueue, c => new ProcessPayment
                {
                    PaymentId = c.Message.PaymentId,
                    OrderId = c.Saga.CorrelationId,
                    Amount = c.Saga.Amount,
                    CardLast4 = c.Message.CardLast4
                })
                // Zaman aşımı sürer: ödemenin cevabı süre dolmadan gelmezse sipariş düşer.
                .TransitionTo(PaymentProcessing),
            When(CancellationRequested)
                .Unschedule(Expiry)
                .Send(ReleaseSeatsQueue, c => ReleaseSeatsFor(c.Saga))
                .ThenAsync(c => SetOrderStatusAsync(c, OrderStatus.Cancelled, "Siparişten vazgeçildi."))
                .Finalize(),
            When(Expiry.Received)
                .Send(ReleaseSeatsQueue, c => ReleaseSeatsFor(c.Saga))
                .ThenAsync(c => SetOrderStatusAsync(c, OrderStatus.Expired, "Ödeme süresi doldu."))
                .Finalize(),
            // Aynı cevap ikinci kez gelirse.
            Ignore(SeatsReserved));

        During(PaymentProcessing,
            When(PaymentSucceeded)
                // Para alındı; koltuklar süre dolsa da satılmaya çalışılır (bkz. Reservation ConfirmHoldCommandHandler).
                .Unschedule(Expiry)
                .Send(ConfirmSeatsQueue, c => new ConfirmSeats { OrderId = c.Saga.CorrelationId })
                .TransitionTo(SeatsConfirming),
            When(PaymentFailed)
                // Kart reddedildi: sipariş açık kalır, müşteri süre dolana kadar yeniden dener.
                .ThenAsync(c => SetOrderStatusAsync(c, OrderStatus.AwaitingPayment, c.Message.Reason))
                .TransitionTo(PaymentPending),
            When(Expiry.Received)
                // Ödemenin cevabı gelmedi (sağlayıcı cevap vermedi, Payment kapalı). Para çekildiyse ya da sonradan
                // çekilirse Payment iade eder.
                .Send(RefundPaymentQueue, c => new RefundPayment { OrderId = c.Saga.CorrelationId })
                .Send(ReleaseSeatsQueue, c => ReleaseSeatsFor(c.Saga))
                .ThenAsync(c => SetOrderStatusAsync(c, OrderStatus.Expired, "Ödeme süresi doldu; çekilen bir tutar varsa iade edilecek."))
                .Finalize(),
            // Ödeme sürerken vazgeçilemez ve ikinci ödeme başlatılamaz (bkz. CancelOrderCommandHandler, PayOrderCommandHandler).
            Ignore(CancellationRequested),
            Ignore(PaymentRequested),
            Ignore(SeatsReserved));

        During(SeatsConfirming,
            When(SeatsConfirmed)
                .SendAsync(IssueTicketsQueue, IssueTicketsForAsync)
                .TransitionTo(TicketsIssuing),
            When(SeatConfirmationFailed)
                // Koltuklar satılamadı (tutma bırakılmış, koltuk başka siparişe satılmış): para iade edilir.
                .Send(RefundPaymentQueue, c => new RefundPayment { OrderId = c.Saga.CorrelationId })
                .Send(ReleaseSeatsQueue, c => ReleaseSeatsFor(c.Saga))
                .ThenAsync(c => SetOrderStatusAsync(c, OrderStatus.Cancelled, $"{c.Message.Reason} Ödemeniz iade edilecek."))
                .Finalize(),
            // Zaman aşımı iptal edildi ama iptalden hemen önce tetiklenmiş olabilir.
            Ignore(Expiry.Received),
            Ignore(CancellationRequested),
            Ignore(PaymentRequested),
            Ignore(PaymentSucceeded));

        During(TicketsIssuing,
            When(TicketsIssued)
                .ThenAsync(c => SetOrderStatusAsync(c, OrderStatus.Completed, null))
                .Finalize(),
            When(TicketIssueFailed)
                // Bilet çıkarılamadı (etkinlik bu arada iptal edildi…): para iade edilir, satılan koltuklar geri açılır.
                .Send(RefundPaymentQueue, c => new RefundPayment { OrderId = c.Saga.CorrelationId })
                .Send(ReleaseSeatsQueue, c => ReleaseSeatsFor(c.Saga))
                .ThenAsync(c => SetOrderStatusAsync(c, OrderStatus.Cancelled, $"{c.Message.Reason} Ödemeniz iade edilecek."))
                .Finalize(),
            Ignore(Expiry.Received),
            Ignore(CancellationRequested),
            Ignore(PaymentRequested),
            Ignore(PaymentSucceeded),
            Ignore(SeatsConfirmed));

        // Kapanan saga silinir; sipariş (orders) geçmiş olarak kalır.
        SetCompletedWhenFinalized();
    }

    private static ReleaseSeats ReleaseSeatsFor(OrderState saga) => new()
    {
        OrderId = saga.CorrelationId,
        EventId = saga.EventId,
        CustomerId = saga.CustomerId
    };

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

    // Bilet ve PDF Catalog'a sormadan çıksın diye etkinlik bilgisi ve koltuklar siparişten kopyalanır.
    private static async Task<IssueTickets> IssueTicketsForAsync(BehaviorContext<OrderState, SeatsConfirmed> context)
    {
        var order = await GetOrderAsync(context, context.Saga.CorrelationId);
        return new IssueTickets
        {
            OrderId = order.Id,
            EventId = order.EventId,
            CustomerId = order.CustomerId,
            CustomerEmail = order.CustomerEmail,
            CustomerName = order.CustomerName,
            EventTitle = order.EventTitle,
            EventStartsAt = order.EventStartsAt,
            VenueName = order.VenueName,
            City = order.City,
            Seats = order.Items.Select(item => new TicketSeat
            {
                SeatId = item.SeatId,
                SectionName = item.SectionName,
                Row = item.Row,
                Number = item.Number,
                Price = item.Price
            }).ToList()
        };
    }

    private static async Task MarkPaymentProcessingAsync(BehaviorContext<OrderState, OrderPaymentRequested> context)
    {
        var order = await GetOrderAsync(context, context.Saga.CorrelationId);
        order.Status = OrderStatus.PaymentProcessing;
        order.FailureReason = null;
        order.LastPaymentId = context.Message.PaymentId;
    }

    private static async Task SetOrderStatusAsync<TMessage>(BehaviorContext<OrderState, TMessage> context, OrderStatus status, string? reason)
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
