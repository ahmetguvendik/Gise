using MediatR;

namespace Gise.Order.Application.Features.Commands.Order.Request;

// Etkinlik iptal edildi (Kafka'dan EventCancelled, order-inbox-worker): tamamlanmış siparişlerin parası iade edilir.
// Ödemesi süren siparişler saga'nın kendi telafisiyle iade edilir (koltuk satışı ya da bilet adımı reddedilir).
public sealed class RefundEventOrdersCommand : IRequest
{
    public Guid EventId { get; set; }
}
