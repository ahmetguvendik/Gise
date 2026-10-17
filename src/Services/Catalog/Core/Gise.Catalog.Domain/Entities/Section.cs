using Gise.Catalog.Domain.Common;
using Gise.Catalog.Domain.Exceptions;

namespace Gise.Catalog.Domain.Entities;

// Mekânın bir bölümü: "A Blok", "Balkon". Fiyat bölüm bazında belirlenir (bkz. PriceCategory).
public sealed class Section : BaseEntity
{
    public const int NameMaxLength = 100;
    public const int MaxSeatsPerRow = 100;

    private readonly List<Seat> _seats = [];

    public Guid VenueId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public IReadOnlyCollection<Seat> Seats => _seats;

    private Section() { }

    // Yalnızca Venue.AddSection üzerinden: aynı mekânda aynı adlı bölüm kontrolü orada.
    internal static Section Create(Guid venueId, string name)
    {
        name = name.Trim();

        if (name.Length is 0 or > NameMaxLength)
            throw new DomainException($"Bölüm adı boş olamaz ve en fazla {NameMaxLength} karakter olabilir.");

        return new Section { VenueId = venueId, Name = name };
    }

    // Sıradaki koltuklar 1'den başlayarak numaralanır.
    public void AddRow(string row, int seatCount)
    {
        row = row.Trim();

        if (seatCount is < 1 or > MaxSeatsPerRow)
            throw new DomainException($"Bir sırada 1 ile {MaxSeatsPerRow} arasında koltuk olabilir.");

        if (_seats.Any(s => string.Equals(s.Row, row, StringComparison.OrdinalIgnoreCase)))
            throw new DomainException($"'{Name}' bölümünde '{row}' sırası zaten var.");

        for (var number = 1; number <= seatCount; number++)
            _seats.Add(Seat.Create(Id, row, number));
    }
}
