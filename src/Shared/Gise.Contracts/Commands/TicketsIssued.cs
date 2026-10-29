namespace Gise.Contracts.Commands;

// Ticket → Order: siparişin biletleri çıktı.
public sealed class TicketsIssued
{
    public Guid OrderId { get; set; }
}
