namespace Gise.Contracts.Commands;

// Payment → Order: para çekildi.
public sealed class PaymentSucceeded
{
    public Guid PaymentId { get; set; }
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
}
