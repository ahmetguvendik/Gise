using System.Net;
using System.Net.Http.Json;
using Gise.Catalog.Application.Caching;
using Gise.Catalog.Application.Features.Queries.Event.Response;
using Gise.Catalog.IntegrationTests.Fixtures;
using Gise.Catalog.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gise.Catalog.IntegrationTests.Caching;

// Etkinlik detayı (GET /api/events/{id}) gerçek Redis ve gerçek veritabanıyla: isabet, silme ve taslağın sızmaması.
public sealed class EventCachingTests(CatalogApiFactory factory) : IClassFixture<CatalogApiFactory>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Ikinci_okuma_veritabanina_gitmeden_onbellekten_gelir()
    {
        var (eventId, _, _) = await CatalogScenarios.CreatePublishedEventAsync(factory, "Eski Ad");
        var anonymous = factory.CreateClient();

        var first = await anonymous.GetFromJsonAsync<GetEventByIdResponse>($"/api/events/{eventId}", Ct);
        Assert.Equal("Eski Ad", first!.Title);
        Assert.True(await factory.Cache.KeyExistsAsync(CacheKeys.Event(eventId)));

        // Komutları atlayıp veritabanını doğrudan değiştiriyoruz: yanıt hâlâ eskiyse önbellekten geliyor demektir.
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            await db.Events.Where(e => e.Id == eventId).ExecuteUpdateAsync(s => s.SetProperty(e => e.Title, "Yeni Ad"), Ct);
        }

        var cached = await anonymous.GetFromJsonAsync<GetEventByIdResponse>($"/api/events/{eventId}", Ct);
        Assert.Equal("Eski Ad", cached!.Title);

        await factory.Cache.KeyDeleteAsync(CacheKeys.Event(eventId));
        var fresh = await anonymous.GetFromJsonAsync<GetEventByIdResponse>($"/api/events/{eventId}", Ct);
        Assert.Equal("Yeni Ad", fresh!.Title);
    }

    [Fact]
    public async Task Fiyat_degisince_etkinlik_onbellegi_temizlenir()
    {
        var (eventId, _, sectionId) = await CatalogScenarios.CreatePublishedEventAsync(factory, price: 100m);
        var anonymous = factory.CreateClient();
        await anonymous.GetFromJsonAsync<GetEventByIdResponse>($"/api/events/{eventId}", Ct);
        Assert.True(await factory.Cache.KeyExistsAsync(CacheKeys.Event(eventId)));

        var changed = await factory.CreateClientAs("admin-1", "admin")
            .PutAsJsonAsync($"/api/events/{eventId}/prices", new { prices = new[] { new { sectionId, price = 150m } } }, Ct);
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        Assert.False(await factory.Cache.KeyExistsAsync(CacheKeys.Event(eventId)));

        var fresh = await anonymous.GetFromJsonAsync<GetEventByIdResponse>($"/api/events/{eventId}", Ct);
        Assert.Equal(150m, fresh!.Prices.Single().Price);
    }

    [Fact]
    public async Task Iptal_edilince_etkinlik_onbellegi_temizlenir_ve_iptal_gorunur()
    {
        var (eventId, _, _) = await CatalogScenarios.CreatePublishedEventAsync(factory);
        var anonymous = factory.CreateClient();
        var before = await anonymous.GetFromJsonAsync<GetEventByIdResponse>($"/api/events/{eventId}", Ct);
        Assert.Equal("Published", before!.Status);

        var cancelled = await factory.CreateClientAs("admin-1", "admin")
            .PostAsJsonAsync($"/api/events/{eventId}/cancel", new { reason = "Sahne çöktü" }, Ct);
        Assert.Equal(HttpStatusCode.OK, cancelled.StatusCode);
        Assert.False(await factory.Cache.KeyExistsAsync(CacheKeys.Event(eventId)));

        var after = await anonymous.GetFromJsonAsync<GetEventByIdResponse>($"/api/events/{eventId}", Ct);
        Assert.Equal("Cancelled", after!.Status);
        Assert.Equal("Sahne çöktü", after.CancellationReason);
    }

    // Taslağı yalnızca sahibi ve admin görür (başkasına 404). Sahibin okuması önbelleğe yazılsaydı başkası da görürdü.
    [Fact]
    public async Task Taslak_onbellege_yazilmaz_ve_baskasina_sizmaz()
    {
        var (venueId, _) = await CatalogScenarios.CreateVenueAsync(factory);
        var eventId = await CatalogScenarios.CreateDraftEventAsync(factory, venueId, "Gizli Taslak");

        var own = await factory.CreateClientAs("organizator-1", "organizer").GetAsync($"/api/events/{eventId}", Ct);
        Assert.Equal(HttpStatusCode.OK, own.StatusCode);
        Assert.False(await factory.Cache.KeyExistsAsync(CacheKeys.Event(eventId)));

        var anonymous = await factory.CreateClient().GetAsync($"/api/events/{eventId}", Ct);
        Assert.Equal(HttpStatusCode.NotFound, anonymous.StatusCode);

        var otherOrganizer = await factory.CreateClientAs("baska-organizator", "organizer").GetAsync($"/api/events/{eventId}", Ct);
        Assert.Equal(HttpStatusCode.NotFound, otherOrganizer.StatusCode);

        var admin = await factory.CreateClientAs("admin-1", "admin").GetAsync($"/api/events/{eventId}", Ct);
        Assert.Equal(HttpStatusCode.OK, admin.StatusCode);
    }

    [Fact]
    public async Task Olmayan_etkinlik_onbellege_yazilmaz()
    {
        var id = Guid.NewGuid();

        var response = await factory.CreateClient().GetAsync($"/api/events/{id}", Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.False(await factory.Cache.KeyExistsAsync(CacheKeys.Event(id)));
    }
}
