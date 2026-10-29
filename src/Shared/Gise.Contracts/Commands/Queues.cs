namespace Gise.Contracts.Commands;

// Komutlar belirli bir kuyruğa gönderilir (RabbitMQ, bkz. tasarım 2.1); gönderen ve alan taraf adı buradan alır.
public static class Queues
{
    public const string ReserveSeats = "reserve-seats";
    public const string ReleaseSeats = "release-seats";
    public const string ConfirmSeats = "confirm-seats";
    public const string ProcessPayment = "process-payment";
    public const string RefundPayment = "refund-payment";
}
