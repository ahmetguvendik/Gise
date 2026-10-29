namespace Gise.Ticket.Application.Features.Commands.Ticket.Response;

// Görevlinin ekranında gösterilir: doğru etkinlik ve koltuk mu.
public sealed class CheckInTicketResponse
{
    public Guid TicketId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public DateTime EventStartsAt { get; set; }
    public string SectionName { get; set; } = string.Empty;
    public string Row { get; set; } = string.Empty;
    public int Number { get; set; }
    public DateTime CheckedInAt { get; set; }
}
