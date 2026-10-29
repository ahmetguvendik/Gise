using System.Globalization;
using System.Net;
using System.Text;

namespace Gise.Notification.Application.Emails;

// Bildirim e-postaları. Olaylardan gelen her değer HTML'e kodlanarak yazılır. Saatler etkinliğin yerel saatiyle
// (Notification:TimeZone).
public static class EmailTemplates
{
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    public static Email TicketsIssued(
        string name, string eventTitle, DateTime startsAt, string venueName, string city,
        IReadOnlyCollection<(string Section, string Row, int Number)> seats, string ticketsUrl, TimeZoneInfo timeZone)
    {
        var body = new StringBuilder()
            .Append(Greeting(name))
            .Append($"<p>Siparişiniz tamamlandı, <strong>{Encode(eventTitle)}</strong> için {seats.Count} biletiniz hazır.</p>")
            .Append(EventLine(startsAt, venueName, city, timeZone))
            .Append("<ul>");
        foreach (var seat in seats)
            body.Append($"<li>{Encode(seat.Section)} · Sıra {Encode(seat.Row)} · Koltuk {seat.Number}</li>");
        body.Append("</ul>")
            .Append($"<p><a href=\"{Encode(ticketsUrl)}\">Biletlerim</a> sayfasından PDF'leri indirebilirsiniz. Kapıda PDF'teki QR okutulur; her bilet bir kez geçerlidir.</p>");

        return new Email($"Biletleriniz hazır: {eventTitle}", Wrap(body.ToString()));
    }

    public static Email EventCancelled(
        string name, string eventTitle, DateTime startsAt, string venueName, string city, string reason, TimeZoneInfo timeZone)
    {
        var body = new StringBuilder()
            .Append(Greeting(name))
            .Append($"<p><strong>{Encode(eventTitle)}</strong> iptal edildi.</p>")
            .Append(EventLine(startsAt, venueName, city, timeZone))
            .Append($"<p>Organizatörün açıklaması: {Encode(reason)}</p>")
            .Append("<p>Biletleriniz geçersiz oldu; ödediğiniz tutar kesintisiz iade edildi. Bankanıza bağlı olarak hesabınıza geçmesi birkaç gün sürebilir.</p>");

        return new Email($"Etkinlik iptal edildi: {eventTitle}", Wrap(body.ToString()));
    }

    public static Email EventRescheduled(
        string name, string eventTitle, DateTime previousStartsAt, DateTime startsAt, string venueName, string city, TimeZoneInfo timeZone)
    {
        var body = new StringBuilder()
            .Append(Greeting(name))
            .Append($"<p><strong>{Encode(eventTitle)}</strong> etkinliğinin tarihi değişti.</p>")
            .Append($"<p>Eski tarih: <s>{FormatDate(previousStartsAt, timeZone)}</s><br />Yeni tarih: <strong>{FormatDate(startsAt, timeZone)}</strong><br />{Encode(venueName)}, {Encode(city)}</p>")
            .Append("<p>Biletleriniz yeni tarih için geçerli; bir şey yapmanız gerekmez.</p>");

        return new Email($"Etkinlik tarihi değişti: {eventTitle}", Wrap(body.ToString()));
    }

    private static string Greeting(string name)
        => string.IsNullOrWhiteSpace(name) ? "<p>Merhaba,</p>" : $"<p>Merhaba {Encode(name)},</p>";

    private static string EventLine(DateTime startsAt, string venueName, string city, TimeZoneInfo timeZone)
        => $"<p>{FormatDate(startsAt, timeZone)}<br />{Encode(venueName)}, {Encode(city)}</p>";

    // "20 Aralık 2026 Pazar, 20:00"
    private static string FormatDate(DateTime utc, TimeZoneInfo timeZone)
        => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), timeZone).ToString("d MMMM yyyy dddd, HH:mm", Turkish);

    private static string Encode(string value) => WebUtility.HtmlEncode(value);

    private static string Wrap(string content)
        => $"<div style=\"font-family:Arial,Helvetica,sans-serif;font-size:15px;line-height:1.5;color:#1c1a17;max-width:560px\">{content}<p style=\"color:#6b655c;font-size:13px\">Gişe</p></div>";
}

public sealed record Email(string Subject, string HtmlBody);
