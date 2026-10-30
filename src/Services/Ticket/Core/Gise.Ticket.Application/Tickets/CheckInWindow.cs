using Microsoft.Extensions.Configuration;

namespace Gise.Ticket.Application.Tickets;

// Kapıda okutma etkinlikten belli bir süre önce açılır, başladıktan belli bir süre sonra kapanır (appsettings'teki CheckIn
// bölümü). Etkinliğin bitiş saati sistemde yok; kapanış başlangıçtan sayılır. Pencere biletteki etkinlik tarihinden
// hesaplanır; etkinlik ertelenince bilet güncellendiği için kendiliğinden kayar.
public sealed record CheckInWindow(int OpensHoursBefore, int ClosesHoursAfter)
{
    public static CheckInWindow FromConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetRequiredSection("CheckIn");
        return new CheckInWindow(section.GetValue<int>("OpensHoursBefore"), section.GetValue<int>("ClosesHoursAfter"));
    }

    public DateTime OpensAt(DateTime eventStartsAt) => eventStartsAt.AddHours(-OpensHoursBefore);

    public DateTime ClosesAt(DateTime eventStartsAt) => eventStartsAt.AddHours(ClosesHoursAfter);
}
