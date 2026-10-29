using Gise.Ticket.Application.Features.Commands.Ticket.Response;
using MediatR;
using System.Text.Json.Serialization;

namespace Gise.Ticket.Application.Features.Commands.Ticket.Request;

// Kapıda bilet okutma: geçerli bilet "kullanıldı" olur, ikinci okutma reddedilir.
public sealed class CheckInTicketCommand : IRequest<CheckInTicketResponse>
{
    // QR'ın içeriği ya da biletteki kod (tireli, küçük harfli de olabilir).
    public string Code { get; set; } = string.Empty;

    // Body'den gelmez — controller JWT'den set eder.
    [JsonIgnore]
    public string StaffId { get; set; } = string.Empty;
}
