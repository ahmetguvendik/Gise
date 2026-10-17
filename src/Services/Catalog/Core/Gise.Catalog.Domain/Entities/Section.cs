using Gise.Catalog.Domain.Common;
using Gise.Catalog.Domain.Exceptions;

namespace Gise.Catalog.Domain.Entities;

// Mekânın bir bölümü: "A Blok", "Balkon". Fiyat bölüm bazında belirlenir (bkz. PriceCategory).
public sealed class Section : BaseEntity
{
    public const int NameMaxLength = 100;

    public Guid VenueId { get; private set; }
    public string Name { get; private set; } = string.Empty;

    private Section() { }

    public static Section Create(Guid venueId, string name)
    {
        name = name.Trim();

        if (name.Length is 0 or > NameMaxLength)
            throw new DomainException($"Bölüm adı boş olamaz ve en fazla {NameMaxLength} karakter olabilir.");

        return new Section { VenueId = venueId, Name = name };
    }
}
