using Gise.Catalog.Domain.Exceptions;

namespace Gise.Catalog.Domain.Entities;

public sealed class Event
{
    public const int TitleMaxLength = 200;
    public const int VenueMaxLength = 200;

    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Venue { get; private set; } = string.Empty;
    public DateTime StartsAt { get; private set; }
    public string OrganizerId { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    private Event() { }

    public static Event Create(string title, string venue, DateTime startsAt, string organizerId, DateTime now)
    {
        title = title.Trim();
        venue = venue.Trim();

        if (title.Length is 0 or > TitleMaxLength)
            throw new DomainException("Etkinlik adı boş olamaz ve en fazla 200 karakter olabilir.");

        if (venue.Length is 0 or > VenueMaxLength)
            throw new DomainException("Mekân boş olamaz ve en fazla 200 karakter olabilir.");

        if (startsAt <= now)
            throw new DomainException("Geçmiş tarihli etkinlik oluşturulamaz.");

        return new Event
        {
            Id = Guid.CreateVersion7(),
            Title = title,
            Venue = venue,
            StartsAt = startsAt,
            OrganizerId = organizerId,
            CreatedAt = now
        };
    }
}
