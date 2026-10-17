using Gise.Catalog.Domain.Entities;
using Gise.Catalog.Domain.Exceptions;

namespace Gise.Catalog.UnitTests.Entities;

public sealed class EventTests
{
    private static readonly DateTime Now = new(2026, 10, 17, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_bosluklari_kirpar_ve_alanlari_doldurur()
    {
        var entity = Event.Create("  Duman Konseri  ", "  Harbiye  ", Now.AddDays(10), "organizator-1", Now);

        Assert.NotEqual(Guid.Empty, entity.Id);
        Assert.Equal("Duman Konseri", entity.Title);
        Assert.Equal("Harbiye", entity.Venue);
        Assert.Equal("organizator-1", entity.OrganizerId);
        Assert.Equal(Now, entity.CreatedAt);
    }

    [Theory]
    [InlineData("   ", "Harbiye")]
    [InlineData("Duman Konseri", "")]
    public void Create_bos_ad_ya_da_mekani_reddeder(string title, string venue)
    {
        Assert.Throws<DomainException>(() => Event.Create(title, venue, Now.AddDays(1), "organizator-1", Now));
    }

    [Fact]
    public void Create_gecmis_tarihi_reddeder()
    {
        var exception = Assert.Throws<DomainException>(
            () => Event.Create("Duman Konseri", "Harbiye", Now.AddMinutes(-1), "organizator-1", Now));

        Assert.Equal("Geçmiş tarihli etkinlik oluşturulamaz.", exception.Message);
    }
}
