using System.Text.Json;
using Gise.Catalog.Application.Caching;
using Gise.Catalog.Application.Features.Queries.Event.Request;
using Gise.Catalog.Application.Features.Queries.Event.Response;
using Gise.Catalog.Application.Features.Queries.Venue.Request;

namespace Gise.Catalog.UnitTests.Caching;

// Taslak etkinliğin başkasına sızmaması ve anahtarların tutarlılığı: önbellek kurallarının en kritik noktaları.
public sealed class CacheabilityTests
{
    // Yayındaki ve iptal edilmiş etkinlik herkese aynı görünür; taslak ve onay bekleyen yalnızca sahibine ve admin'e.
    [Theory]
    [InlineData("Published", true)]
    [InlineData("Cancelled", true)]
    [InlineData("Draft", false)]
    [InlineData("PendingReview", false)]
    public void Etkinlik_yaniti_yalnizca_herkese_acik_durumlarda_onbellege_alinabilir(string status, bool expected)
        => Assert.Equal(expected, new GetEventByIdResponse { Status = status }.CanBeCached);

    // CanBeCached önbellek ve API yanıtına girmemeli: istemci yeni bir alan görmez.
    [Fact]
    public void CanBeCached_serilestirmeye_girmez()
    {
        var json = JsonSerializer.Serialize(new GetEventByIdResponse { Status = "Published" }, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.DoesNotContain("canBeCached", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sorgularin_anahtari_komutlarin_sildigi_anahtarla_ayni_yerden_gelir()
    {
        var id = Guid.NewGuid();

        Assert.Equal(CacheKeys.Event(id), new GetEventByIdQuery { Id = id }.CacheKey);
        Assert.Equal(CacheKeys.EventSummary(id), new GetEventSummaryQuery { EventId = id }.CacheKey);
        Assert.Equal(CacheKeys.Venue(id), new GetVenueByIdQuery { Id = id }.CacheKey);
        Assert.Equal(CacheKeys.Venues, new GetAllVenuesQuery().CacheKey);
    }

    // Aynı Redis'te koltuk kilitleri de var: Catalog anahtarları "catalog:" ile başlar, çakışmaz.
    [Fact]
    public void Butun_anahtarlar_catalog_oneklidir()
    {
        var id = Guid.NewGuid();

        Assert.All(new[] { CacheKeys.Event(id), CacheKeys.EventSummary(id), CacheKeys.Venue(id), CacheKeys.Venues },
            key => Assert.StartsWith("catalog:", key));
    }
}
