namespace Gise.Ticket.Application.Features.Queries.Ticket.Response;

public sealed class GetTicketDownloadUrlResponse
{
    public string Url { get; set; } = string.Empty;
    public int ExpiresInSeconds { get; set; }
}
