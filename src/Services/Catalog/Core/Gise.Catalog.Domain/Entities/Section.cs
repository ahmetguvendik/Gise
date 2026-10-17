using Gise.Catalog.Domain.Common;

namespace Gise.Catalog.Domain.Entities;

// Mekânın bir bölümü: "A Blok", "Balkon". Fiyat bölüm bazında belirlenir (bkz. PriceCategory).
public class Section : BaseEntity
{
    public Guid VenueId { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<Seat> Seats { get; set; } = [];
}
