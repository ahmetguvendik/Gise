using System.Text.Json.Serialization;
using Gise.Ticket.Application.Features.Commands.TicketTransfer.Response;
using MediatR;

namespace Gise.Ticket.Application.Features.Commands.TicketTransfer.Request;

// Bileti bir e-posta adresine aktarma teklifi; alıcı o adresle giriş yapıp kabul edince bilet ona geçer.
public sealed class OfferTicketTransferCommand : IRequest<OfferTicketTransferResponse>
{
    // Alıcının e-postası.
    public string Email { get; set; } = string.Empty;

    // Body'den gelmez — controller route'tan ve JWT'den set eder.
    [JsonIgnore]
    public Guid TicketId { get; set; }

    [JsonIgnore]
    public string CustomerId { get; set; } = string.Empty;

    [JsonIgnore]
    public string CustomerEmail { get; set; } = string.Empty;

    [JsonIgnore]
    public string CustomerName { get; set; } = string.Empty;
}
