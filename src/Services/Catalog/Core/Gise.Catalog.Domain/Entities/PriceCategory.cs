using Gise.Catalog.Domain.Common;
using Gise.Catalog.Domain.Exceptions;

namespace Gise.Catalog.Domain.Entities;

// Bir bölümün o etkinlikteki fiyatı. Para birimi şimdilik TRY.
public sealed class PriceCategory : BaseEntity
{
    public const decimal MaxPrice = 1_000_000m;

    public Guid EventId { get; private set; }
    public Guid SectionId { get; private set; }
    public decimal Price { get; private set; }

    private PriceCategory() { }

    // Yalnızca Event.SetPrices üzerinden: bölümün mekâna ait olup olmadığı orada kontrol edilir.
    internal static PriceCategory Create(Guid eventId, Guid sectionId, decimal price)
    {
        EnsureValid(price);
        return new PriceCategory { EventId = eventId, SectionId = sectionId, Price = price };
    }

    internal void ChangePrice(decimal price)
    {
        EnsureValid(price);
        Price = price;
    }

    private static void EnsureValid(decimal price)
    {
        if (price is <= 0 or > MaxPrice)
            throw new DomainException("Fiyat 0'dan büyük ve en fazla 1.000.000 ₺ olmalıdır.");

        if (decimal.Round(price, 2) != price)
            throw new DomainException("Fiyat en fazla 2 ondalık basamak içerebilir.");
    }
}
