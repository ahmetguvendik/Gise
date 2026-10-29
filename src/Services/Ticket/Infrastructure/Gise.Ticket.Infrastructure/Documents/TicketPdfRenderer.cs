using System.Globalization;
using Gise.Ticket.Application.Interfaces;
using Gise.Ticket.Application.Tickets;
using Microsoft.Extensions.Configuration;
using QRCoder;
using SkiaSharp;

namespace Gise.Ticket.Infrastructure.Documents;

// Tek sayfalık bilet: A6 dikey. Üstte etkinlik, ortada koltuk, altta QR ve kod. Yazı ve QR vektör olarak çizilir;
// yazdırınca da keskin kalır. Ayarlar appsettings'teki TicketPdf bölümünden okunur.
public sealed class TicketPdfRenderer : ITicketPdfRenderer, IDisposable
{
    // A6 (105 × 148 mm), PDF birimi nokta (1/72 inç).
    private const float PageWidth = 298;
    private const float PageHeight = 420;
    private const float Margin = 22;
    private const float QrSize = 150;

    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");
    private static readonly SKColor Ink = new(0x1C, 0x1A, 0x17);
    private static readonly SKColor Muted = new(0x6B, 0x65, 0x5C);
    private static readonly SKColor Line = new(0xD9, 0xD3, 0xC9);
    private static readonly SKColor Accent = new(0xE0, 0x5A, 0x2B);

    private readonly SKTypeface _regular;
    private readonly SKTypeface _bold;
    private readonly TimeZoneInfo _timeZone;

    public TicketPdfRenderer(IConfiguration configuration)
    {
        var pdf = configuration.GetRequiredSection("TicketPdf");
        _regular = LoadTypeface(pdf["FontPath"], "TicketPdf:FontPath");
        _bold = LoadTypeface(pdf["BoldFontPath"], "TicketPdf:BoldFontPath");
        // Etkinlik saati UTC saklanır; bilette yerel saatle yazılır.
        _timeZone = TimeZoneInfo.FindSystemTimeZoneById(pdf["TimeZone"] ?? throw new InvalidOperationException("TicketPdf:TimeZone ayarı eksik."));
    }

    // Türkçe karakterleri içeren bir yazı tipi gerekir; konteynerde fonts-dejavu-core paketiyle gelir.
    private static SKTypeface LoadTypeface(string? path, string setting)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new InvalidOperationException($"{setting} ayarı eksik.");

        return SKTypeface.FromFile(path) ?? throw new InvalidOperationException($"Yazı tipi açılamadı: {path}");
    }

    public byte[] Render(Domain.Entities.Ticket ticket)
    {
        using var stream = new SKDynamicMemoryWStream();
        using (var document = SKDocument.CreatePdf(stream, new SKDocumentPdfMetadata
        {
            Title = $"{ticket.EventTitle} — Bilet",
            Creator = "Gişe",
            Producer = "Gişe"
        }))
        {
            var canvas = document.BeginPage(PageWidth, PageHeight);
            Draw(canvas, ticket);
            document.EndPage();
            document.Close();
        }

        using var data = stream.DetachAsData();
        return data.ToArray();
    }

    private void Draw(SKCanvas canvas, Domain.Entities.Ticket ticket)
    {
        using var fill = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill };
        using var small = new SKFont(_regular, 8);
        using var label = new SKFont(_bold, 7);
        using var body = new SKFont(_regular, 10);
        using var title = new SKFont(_bold, 17);
        using var value = new SKFont(_bold, 15);
        using var code = new SKFont(_bold, 12);

        // Üst şerit.
        fill.Color = Ink;
        canvas.DrawRect(0, 0, PageWidth, 44, fill);
        fill.Color = SKColors.White;
        DrawText(canvas, "GİŞE", Margin, 28, SKTextAlign.Left, title, fill);
        fill.Color = Accent;
        DrawText(canvas, "BİLET", PageWidth - Margin, 27, SKTextAlign.Right, label, fill);

        // Etkinlik.
        var y = 72f;
        fill.Color = Ink;
        foreach (var line in Wrap(ticket.EventTitle, title, PageWidth - 2 * Margin).Take(2))
        {
            DrawText(canvas, line, Margin, y, SKTextAlign.Left, title, fill);
            y += 21;
        }

        var startsAt = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(ticket.EventStartsAt, DateTimeKind.Utc), _timeZone);
        fill.Color = Muted;
        DrawText(canvas, startsAt.ToString("d MMMM yyyy dddd · HH:mm", Turkish), Margin, y, SKTextAlign.Left, body, fill);
        y += 14;
        DrawText(canvas, $"{ticket.VenueName}, {ticket.City}", Margin, y, SKTextAlign.Left, body, fill);

        // Koltuk: üç sütun.
        y += 20;
        DrawRule(canvas, fill, y);
        y += 18;
        var columnWidth = (PageWidth - 2 * Margin) / 3;
        DrawField(canvas, fill, label, value, "BÖLÜM", ticket.SectionName, Margin, y, columnWidth);
        DrawField(canvas, fill, label, value, "SIRA", ticket.Row, Margin + columnWidth, y, columnWidth);
        DrawField(canvas, fill, label, value, "KOLTUK", ticket.Number.ToString(Turkish), Margin + 2 * columnWidth, y, columnWidth);
        y += 30;
        DrawRule(canvas, fill, y);

        // QR ve kod.
        var qrTop = y + 16;
        DrawQr(canvas, fill, ticket.Code, (PageWidth - QrSize) / 2, qrTop, QrSize);
        fill.Color = Ink;
        DrawText(canvas, TicketCode.Format(ticket.Code), PageWidth / 2, qrTop + QrSize + 18, SKTextAlign.Center, code, fill);

        // Alt bilgi.
        fill.Color = Muted;
        DrawText(canvas, $"Fiyat: {ticket.Price.ToString("C", Turkish)} · Kapıda bir kez okutulur.", PageWidth / 2, PageHeight - 26, SKTextAlign.Center, small, fill);
        DrawText(canvas, $"Sipariş {ticket.OrderId}", PageWidth / 2, PageHeight - 14, SKTextAlign.Center, small, fill);
    }

    // Yazı, yazı tipi gömülmeden şekil (path) olarak çizilir: Skia'nın Linux derlemesi yazı tipini alt kümelemeden tümüyle
    // gömüyor, bilet başına 700 KB'ı geçiyordu. PDF'teki metin seçilemez; makinenin okuduğu kısım QR.
    private static void DrawText(SKCanvas canvas, string text, float x, float y, SKTextAlign align, SKFont font, SKPaint paint)
    {
        var width = font.MeasureText(text);
        var left = align switch
        {
            SKTextAlign.Center => x - width / 2,
            SKTextAlign.Right => x - width,
            _ => x
        };

        using var path = font.GetTextPath(text, new SKPoint(left, y));
        canvas.DrawPath(path, paint);
    }

    private static void DrawRule(SKCanvas canvas, SKPaint fill, float y)
    {
        fill.Color = Line;
        canvas.DrawRect(Margin, y, PageWidth - 2 * Margin, 0.75f, fill);
    }

    private static void DrawField(SKCanvas canvas, SKPaint fill, SKFont label, SKFont value, string caption, string text, float x, float y, float width)
    {
        fill.Color = Muted;
        DrawText(canvas, caption, x, y, SKTextAlign.Left, label, fill);
        fill.Color = Ink;
        DrawText(canvas, Wrap(text, value, width - 6).FirstOrDefault() ?? string.Empty, x, y + 18, SKTextAlign.Left, value, fill);
    }

    // Modüller tek tek kare olarak çizilir. Kenardaki boşluk (quiet zone) QRCoder'ın matrisinde zaten var.
    private static void DrawQr(SKCanvas canvas, SKPaint fill, string content, float x, float y, float size)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.M);
        var matrix = data.ModuleMatrix;
        var module = size / matrix.Count;

        fill.Color = SKColors.White;
        canvas.DrawRect(x, y, size, size, fill);
        fill.Color = SKColors.Black;
        for (var row = 0; row < matrix.Count; row++)
        {
            for (var column = 0; column < matrix[row].Count; column++)
            {
                if (matrix[row][column])
                    canvas.DrawRect(x + column * module, y + row * module, module, module, fill);
            }
        }
    }

    // Kelime sınırından satırlara böler; tek kelime sığmıyorsa olduğu gibi kalır.
    private static IEnumerable<string> Wrap(string text, SKFont font, float maxWidth)
    {
        var line = string.Empty;
        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = line.Length == 0 ? word : $"{line} {word}";
            if (line.Length > 0 && font.MeasureText(candidate) > maxWidth)
            {
                yield return line;
                line = word;
            }
            else
            {
                line = candidate;
            }
        }

        if (line.Length > 0)
            yield return line;
    }

    public void Dispose()
    {
        _regular.Dispose();
        _bold.Dispose();
    }
}
