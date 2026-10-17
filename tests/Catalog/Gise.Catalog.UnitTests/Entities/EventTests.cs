using Gise.Catalog.Domain.Entities;
using Gise.Catalog.Domain.Exceptions;

namespace Gise.Catalog.UnitTests.Entities;

public sealed class EventTests
{
    private static readonly DateTime Now = new(2026, 10, 17, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid VenueId = Guid.NewGuid();

    [Fact]
    public void Create_bosluklari_kirpar_ve_alanlari_doldurur()
    {
        var entity = Event.Create("  Duman Konseri  ", VenueId, Now.AddDays(10), "organizator-1", Now);

        Assert.NotEqual(Guid.Empty, entity.Id);
        Assert.Equal("Duman Konseri", entity.Title);
        Assert.Equal(VenueId, entity.VenueId);
        Assert.Equal("organizator-1", entity.OrganizerId);
        Assert.Equal(Domain.Enums.EventStatus.Draft, entity.Status);
    }

    [Fact]
    public void Create_bos_adi_reddeder()
    {
        Assert.Throws<DomainException>(() => Event.Create("   ", VenueId, Now.AddDays(1), "organizator-1", Now));
    }

    [Fact]
    public void Create_gecmis_tarihi_reddeder()
    {
        var exception = Assert.Throws<DomainException>(
            () => Event.Create("Duman Konseri", VenueId, Now.AddMinutes(-1), "organizator-1", Now));

        Assert.Equal("Geçmiş tarihli etkinlik oluşturulamaz.", exception.Message);
    }
}
