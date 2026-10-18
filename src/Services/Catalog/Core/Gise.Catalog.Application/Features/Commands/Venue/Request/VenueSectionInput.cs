using Gise.Catalog.Domain.Entities;

namespace Gise.Catalog.Application.Features.Commands.Venue.Request;

// Mekân oluşturma ve düzen güncellemede aynı girdi: bölümler ve sıraları; koltuklar sıradan üretilir.
public sealed class VenueSectionInput
{
    public string Name { get; set; } = string.Empty;
    public List<VenueRowInput> Rows { get; set; } = [];
}

public sealed class VenueRowInput
{
    public string Row { get; set; } = string.Empty;
    public int SeatCount { get; set; }
}

public static class VenueSectionInputExtensions
{
    // Her sıradaki koltuklar 1'den başlayarak numaralanır.
    public static List<Section> ToSections(this IEnumerable<VenueSectionInput> sections)
        => sections
            .Select(section => new Section
            {
                Name = section.Name.Trim(),
                Seats = section.Rows
                    .SelectMany(row => Enumerable.Range(1, row.SeatCount)
                        .Select(number => new Seat { Row = row.Row.Trim(), Number = number }))
                    .ToList()
            })
            .ToList();
}
