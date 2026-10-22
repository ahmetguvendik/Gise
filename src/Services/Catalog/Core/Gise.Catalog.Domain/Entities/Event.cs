using Gise.Catalog.Domain.Common;
using Gise.Catalog.Domain.Enums;

namespace Gise.Catalog.Domain.Entities;

public class Event : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public Guid VenueId { get; set; }
    public DateTime StartsAt { get; set; }
    public string OrganizerId { get; set; } = string.Empty;
    public EventStatus Status { get; set; } = EventStatus.Draft;
    // Admin taslağa geri gönderirken yazar; organizatör yeniden gönderince silinir.
    public string? ReturnReason { get; set; }

    // İptal edilince dolar; sebep etkinlik sayfasında gösterilir, bilet sahiplerine iletilir.
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }

    public ICollection<PriceCategory> PriceCategories { get; set; } = [];
}
