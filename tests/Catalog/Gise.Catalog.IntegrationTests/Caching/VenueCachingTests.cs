using System.Net;
using System.Net.Http.Json;
using Gise.Catalog.Application.Caching;
using Gise.Catalog.Application.Features.Queries.Event.Request;
using Gise.Catalog.Application.Features.Queries.Venue.Response;
using Gise.Catalog.IntegrationTests.Fixtures;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Gise.Catalog.IntegrationTests.Caching;

// Mekân listesi ve detayı ile etkinlik özeti (gRPC'nin sorgusu): komutların doğru anahtarları sildiği.
public sealed class VenueCachingTests(CatalogApiFactory factory) : IClassFixture<CatalogApiFactory>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Yeni_mekan_listeye_hemen_girer()
    {
        var anonymous = factory.CreateClient();
        await anonymous.GetFromJsonAsync<List<GetAllVenuesResponse>>("/api/venues", Ct);
        Assert.True(await factory.Cache.KeyExistsAsync(CacheKeys.Venues));

        var (venueId, _) = await CatalogScenarios.CreateVenueAsync(factory, "Yeni Mekân");

        var list = await anonymous.GetFromJsonAsync<List<GetAllVenuesResponse>>("/api/venues", Ct);
        Assert.Contains(list!, v => v.Id == venueId);
    }

    // Mekân yanıtı HasEvents taşır: ilk etkinlik açılınca düzen kilitlenir; önbellek bunu eski göstermemeli.
    [Fact]
    public async Task Etkinlik_acilinca_mekanin_HasEvents_bilgisi_guncellenir()
    {
        var (venueId, _) = await CatalogScenarios.CreateVenueAsync(factory);
        var anonymous = factory.CreateClient();

        var before = await anonymous.GetFromJsonAsync<GetVenueByIdResponse>($"/api/venues/{venueId}", Ct);
        Assert.False(before!.HasEvents);
        Assert.True(await factory.Cache.KeyExistsAsync(CacheKeys.Venue(venueId)));

        await CatalogScenarios.CreateDraftEventAsync(factory, venueId);
        Assert.False(await factory.Cache.KeyExistsAsync(CacheKeys.Venue(venueId)));

        var after = await anonymous.GetFromJsonAsync<GetVenueByIdResponse>($"/api/venues/{venueId}", Ct);
        Assert.True(after!.HasEvents);

        var list = await anonymous.GetFromJsonAsync<List<GetAllVenuesResponse>>("/api/venues", Ct);
        Assert.True(list!.Single(v => v.Id == venueId).HasEvents);
    }

    [Fact]
    public async Task Taslak_silinince_mekanin_HasEvents_bilgisi_geri_gelir()
    {
        var (venueId, _) = await CatalogScenarios.CreateVenueAsync(factory);
        var eventId = await CatalogScenarios.CreateDraftEventAsync(factory, venueId);
        var anonymous = factory.CreateClient();
        var locked = await anonymous.GetFromJsonAsync<GetVenueByIdResponse>($"/api/venues/{venueId}", Ct);
        Assert.True(locked!.HasEvents);

        var deleted = await factory.CreateClientAs("organizator-1", "organizer").DeleteAsync($"/api/events/{eventId}", Ct);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        var unlocked = await anonymous.GetFromJsonAsync<GetVenueByIdResponse>($"/api/venues/{venueId}", Ct);
        Assert.False(unlocked!.HasEvents);
    }

    // Mekân adı etkinlik özetine de girer (Order siparişe kopyalar): ad değişince mekândaki etkinliklerin özeti silinmeli.
    [Fact]
    public async Task Mekan_adi_degisince_liste_detay_ve_etkinlik_ozeti_yenilenir()
    {
        var (eventId, venueId, _) = await CatalogScenarios.CreatePublishedEventAsync(factory, "Ad Değişimi");
        var anonymous = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        await anonymous.GetFromJsonAsync<List<GetAllVenuesResponse>>("/api/venues", Ct);
        await anonymous.GetFromJsonAsync<GetVenueByIdResponse>($"/api/venues/{venueId}", Ct);
        var summaryBefore = await mediator.Send(new GetEventSummaryQuery { EventId = eventId }, Ct);
        Assert.Equal("Önbellek Sahnesi", summaryBefore.VenueName);
        Assert.True(await factory.Cache.KeyExistsAsync(CacheKeys.EventSummary(eventId)));

        var renamed = await factory.CreateClientAs("admin-1", "admin")
            .PutAsJsonAsync($"/api/venues/{venueId}", new { name = "Yeni Sahne Adı", city = "İzmir" }, Ct);
        Assert.Equal(HttpStatusCode.OK, renamed.StatusCode);

        Assert.False(await factory.Cache.KeyExistsAsync(CacheKeys.Venue(venueId)));
        Assert.False(await factory.Cache.KeyExistsAsync(CacheKeys.Venues));
        Assert.False(await factory.Cache.KeyExistsAsync(CacheKeys.EventSummary(eventId)));

        var summaryAfter = await mediator.Send(new GetEventSummaryQuery { EventId = eventId }, Ct);
        Assert.Equal("Yeni Sahne Adı", summaryAfter.VenueName);
        Assert.Equal("İzmir", summaryAfter.City);

        var venue = await anonymous.GetFromJsonAsync<GetVenueByIdResponse>($"/api/venues/{venueId}", Ct);
        Assert.Equal("Yeni Sahne Adı", venue!.Name);
    }

    // Yalnızca satıştaki etkinliğin özeti saklanır; taslak için sorgu hata verir ve hiçbir şey yazılmaz.
    [Fact]
    public async Task Etkinlik_ozeti_yalnizca_satistaki_etkinlik_icin_saklanir()
    {
        var (venueId, _) = await CatalogScenarios.CreateVenueAsync(factory);
        var draftId = await CatalogScenarios.CreateDraftEventAsync(factory, venueId);
        await using var scope = factory.Services.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        await Assert.ThrowsAnyAsync<Exception>(() => mediator.Send(new GetEventSummaryQuery { EventId = draftId }, Ct));

        Assert.False(await factory.Cache.KeyExistsAsync(CacheKeys.EventSummary(draftId)));
    }
}
