using Gise.Catalog.Domain.Common;
using Gise.Catalog.Domain.Exceptions;

namespace Gise.Catalog.Domain.Entities;

// Bir bölümün o etkinlikteki fiyatı. Para birimi şimdilik TRY.
public sealed class PriceCategory : BaseEntity
{
    public Guid EventId { get; private set; }
    public Guid SectionId { get; private set; }
    public decimal Price { get; private set; }

    private PriceCategory() { }

    public static PriceCategory Create(Guid eventId, Guid sectionId, decimal price)
    {
        if (price <= 0)
            throw new DomainException("Fiyat sıfırdan büyük olmalıdır.");

        return new PriceCategory { EventId = eventId, SectionId = sectionId, Price = price };
    }
}
