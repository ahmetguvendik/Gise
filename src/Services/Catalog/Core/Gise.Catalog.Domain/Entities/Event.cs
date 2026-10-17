using Gise.Catalog.Domain.Common;
using Gise.Catalog.Domain.Enums;
using Gise.Catalog.Domain.Exceptions;

namespace Gise.Catalog.Domain.Entities;

public sealed class Event : BaseEntity
{
    public const int TitleMaxLength = 200;

    private readonly List<PriceCategory> _priceCategories = [];

    public string Title { get; private set; } = string.Empty;
    public Guid VenueId { get; private set; }
    public DateTime StartsAt { get; private set; }
    public string OrganizerId { get; private set; } = string.Empty;
    public EventStatus Status { get; private set; }
    public IReadOnlyCollection<PriceCategory> PriceCategories => _priceCategories;

    private Event() { }

    public static Event Create(string title, Guid venueId, DateTime startsAt, string organizerId, DateTime now)
    {
        title = title.Trim();

        if (title.Length is 0 or > TitleMaxLength)
            throw new DomainException($"Etkinlik adı boş olamaz ve en fazla {TitleMaxLength} karakter olabilir.");

        if (startsAt <= now)
            throw new DomainException("Geçmiş tarihli etkinlik oluşturulamaz.");

        return new Event
        {
            Title = title,
            VenueId = venueId,
            StartsAt = startsAt,
            OrganizerId = organizerId,
            Status = EventStatus.Draft
        };
    }

    // Fiyat listesi bütün olarak değiştirilir: listede olmayan bölümün fiyatı kalkar.
    // Yayındaki etkinlikte de değişebilir; satılmış biletler sipariş anındaki fiyatı korur.
    public void SetPrices(IReadOnlyCollection<(Guid SectionId, decimal Price)> prices, IReadOnlySet<Guid> venueSectionIds)
    {
        if (Status == EventStatus.Cancelled)
            throw new DomainException("İptal edilmiş etkinliğin fiyatları değiştirilemez.");

        if (prices.Select(p => p.SectionId).Distinct().Count() != prices.Count)
            throw new DomainException("Bir bölümün yalnızca bir fiyatı olabilir.");

        if (prices.Any(p => !venueSectionIds.Contains(p.SectionId)))
            throw new DomainException("Fiyat girilen bölümlerden biri bu etkinliğin mekânına ait değil.");

        _priceCategories.RemoveAll(existing => prices.All(p => p.SectionId != existing.SectionId));

        foreach (var (sectionId, price) in prices)
        {
            var existing = _priceCategories.FirstOrDefault(p => p.SectionId == sectionId);
            if (existing is null)
                _priceCategories.Add(PriceCategory.Create(Id, sectionId, price));
            else
                existing.ChangePrice(price);
        }
    }

    public void Publish()
    {
        if (Status != EventStatus.Draft)
            throw new DomainException("Yalnızca taslak etkinlik yayınlanabilir.");

        Status = EventStatus.Published;
    }
}
