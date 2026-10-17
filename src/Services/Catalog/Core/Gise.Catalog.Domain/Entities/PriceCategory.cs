using Gise.Catalog.Domain.Common;

namespace Gise.Catalog.Domain.Entities;

// Bir bölümün o etkinlikteki fiyatı. Para birimi şimdilik TRY.
public class PriceCategory : BaseEntity
{
    public Guid EventId { get; set; }
    public Guid SectionId { get; set; }
    public decimal Price { get; set; }
}
