using MediatR;

namespace Gise.Ticket.Application.Features.Commands.Ticket.Request;

// Siparişin koltukları için bilet çıkarır (Order saga'sının IssueTickets komutu). Tekrar gelirse zararsızdır.
public sealed class IssueTicketsCommand : IRequest
{
    public Guid OrderId { get; set; }
    public Guid EventId { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public string EventTitle { get; set; } = string.Empty;
    public DateTime EventStartsAt { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public List<IssueTicketSeat> Seats { get; set; } = [];
}

public sealed class IssueTicketSeat
{
    public Guid SeatId { get; set; }
    public string SectionName { get; set; } = string.Empty;
    public string Row { get; set; } = string.Empty;
    public int Number { get; set; }
    public decimal Price { get; set; }
}
