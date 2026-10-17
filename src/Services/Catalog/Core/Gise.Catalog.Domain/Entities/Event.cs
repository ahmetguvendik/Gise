using Gise.Catalog.Domain.Common;
using Gise.Catalog.Domain.Enums;
using Gise.Catalog.Domain.Exceptions;

namespace Gise.Catalog.Domain.Entities;

public sealed class Event : BaseEntity
{
    public const int TitleMaxLength = 200;

    public string Title { get; private set; } = string.Empty;
    public Guid VenueId { get; private set; }
    public DateTime StartsAt { get; private set; }
    public string OrganizerId { get; private set; } = string.Empty;
    public EventStatus Status { get; private set; }

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

    public void Publish()
    {
        if (Status != EventStatus.Draft)
            throw new DomainException("Yalnızca taslak etkinlik yayınlanabilir.");

        Status = EventStatus.Published;
    }
}
