namespace Gise.Contracts.Events;

public static class Kafka
{
    // Topic'i olayı yayan servis açılışta oluşturur; dinleyenler yalnızca abone olur.
    public const string CatalogEventsTopic = "catalog.events";
    public const string TicketEventsTopic = "ticket.events";

    // Her mesajın başlıkları: olay tipi (ör. "EventPublished"), tekil mesaj kimliği (alan tarafın inbox'ı
    // tekrarı bununla yakalar) ve isteğin iz bilgisi.
    public const string TypeHeader = "type";
    public const string MessageIdHeader = "message-id";
    public const string TraceParentHeader = "traceparent";
}
