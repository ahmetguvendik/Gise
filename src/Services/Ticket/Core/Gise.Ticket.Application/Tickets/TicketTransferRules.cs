using Microsoft.Extensions.Configuration;

namespace Gise.Ticket.Application.Tickets;

// Aktarım etkinlikten belli bir süre öncesine kadar yapılır (appsettings'teki TicketTransfer:CutoffHours); bekleyen aktarım
// da o an düşer. Son an saklanmaz, biletin güncel etkinlik tarihinden hesaplanır: etkinlik ertelenince kendiliğinden kayar.
public static class TicketTransferRules
{
    public static int CutoffHours(IConfiguration configuration)
        => configuration.GetRequiredSection("TicketTransfer").GetValue<int>("CutoffHours");

    public static DateTime Deadline(Domain.Entities.Ticket ticket, int cutoffHours)
        => ticket.EventStartsAt.AddHours(-cutoffHours);

    // Küçük harf, boşluksuz: alıcının token'ındaki adresle bu biçimde karşılaştırılır.
    public static string NormalizeEmail(string? email) => (email ?? string.Empty).Trim().ToLowerInvariant();
}
