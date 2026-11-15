using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using Gise.Catalog.Application.Features.Commands.Venue.Response;
using Gise.Catalog.Application.Features.Queries.Venue.Response;
using Gise.Catalog.IntegrationTests.Fixtures;

namespace Gise.Catalog.IntegrationTests.Caching;

// Redis düşünce API çalışmaya devam eder: okuma veritabanından, komutlar silemese de kaydeder (Telgraf'taki devre kesici).
public sealed class CacheOutageTests(CatalogApiFactory factory) : IClassFixture<CatalogApiFactory>
{
    [Fact]
    public async Task Redis_kapaliyken_okuma_ve_yazma_veritabanindan_surer()
    {
        var ct = TestContext.Current.CancellationToken;
        var (venueId, _) = await CatalogScenarios.CreateVenueAsync(factory);
        var anonymous = factory.CreateClient();
        await anonymous.GetFromJsonAsync<GetVenueByIdResponse>($"/api/venues/{venueId}", ct);

        await factory.StopRedisAsync();
        var stopwatch = Stopwatch.StartNew();

        var read = await anonymous.GetAsync($"/api/venues/{venueId}", ct);
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);

        var created = await factory.CreateClientAs("admin-1", "admin").PostAsJsonAsync("/api/venues", new
        {
            name = "Redis Kapalıyken",
            city = "Bursa",
            sections = new[] { new { name = "Salon", rows = new[] { new { row = "A", seatCount = 2 } } } }
        }, ct);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var newId = (await created.Content.ReadFromJsonAsync<CreateVenueResponse>(ct))!.Id;

        var list = await anonymous.GetFromJsonAsync<List<GetAllVenuesResponse>>("/api/venues", ct);
        Assert.Contains(list!, v => v.Id == newId);

        // Redis'i beklemeden: bağlantı yokken komut kuyruğa alınmadan düşer, devre kesici de ilk hatadan sonra önbelleği atlar.
        // (Kuyruk beklemesi varken ilk istek 5 sn sürmüştü.)
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(3), $"Redis kapalıyken istekler {stopwatch.Elapsed.TotalSeconds:F1} sn sürdü.");
    }
}
