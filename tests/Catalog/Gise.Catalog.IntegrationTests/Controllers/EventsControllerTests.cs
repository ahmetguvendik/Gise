using System.Net;
using System.Net.Http.Json;
using Gise.Catalog.Application.Features.Commands.Event.Response;
using Gise.Catalog.Application.Features.Commands.Venue.Response;
using Gise.Catalog.Application.Features.Queries.Event.Response;
using Gise.Catalog.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Mvc;

namespace Gise.Catalog.IntegrationTests.Controllers;

public sealed class EventsControllerTests(CatalogApiFactory factory) : IClassFixture<CatalogApiFactory>
{
    // Yetki testleri doğrulamaya ulaşmadan döner; mekânın gerçekten var olması gerekmez.
    private static object NewEvent(string title = "Duman Konseri", Guid? venueId = null) => new
    {
        title,
        venueId = venueId ?? Guid.NewGuid(),
        startsAt = DateTimeOffset.UtcNow.AddDays(30)
    };

    // Etkinlik bir mekânda açılır; mekânı admin oluşturur.
    private async Task<Guid> CreateVenueAsync()
    {
        var admin = factory.CreateClientAs("admin-1", "admin");
        var response = await admin.PostAsJsonAsync("/api/venues", new
        {
            name = "Test Sahnesi",
            city = "Ankara",
            sections = new[] { new { name = "Salon", rows = new[] { new { row = "A", seatCount = 4 } } } }
        }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CreateVenueResponse>(TestContext.Current.CancellationToken))!.Id;
    }

    // Herkese açık liste Search'te (bkz. tasarım 3.10); taslağı yalnızca sahibi ve admin görür.
    [Fact]
    public async Task Organizator_etkinlik_olusturur_ve_taslagi_yalnizca_kendisi_gorur()
    {
        var organizer = factory.CreateClientAs("organizator-1", "organizer");

        var created = await organizer.PostAsJsonAsync("/api/events", NewEvent("Taslak Testi", await CreateVenueAsync()), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var body = await created.Content.ReadFromJsonAsync<CreateEventResponse>(TestContext.Current.CancellationToken);

        var detail = await organizer.GetFromJsonAsync<GetEventByIdResponse>($"/api/events/{body!.Id}", TestContext.Current.CancellationToken);
        Assert.Equal("organizator-1", detail!.OrganizerId);
        Assert.Equal("Taslak Testi", detail.Title);

        var anonymous = await factory.CreateClient().GetAsync($"/api/events/{body.Id}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, anonymous.StatusCode);
    }

    [Fact]
    public async Task Girissiz_olusturma_401_doner()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/events", NewEvent(), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);
        Assert.Equal("Bu işlem için giriş yapmalısınız.", problem!.Title);
    }

    [Fact]
    public async Task Musteri_rolu_ile_olusturma_403_doner()
    {
        var customer = factory.CreateClientAs("musteri-1", "customer");

        var response = await customer.PostAsJsonAsync("/api/events", NewEvent(), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Gecersiz_istek_alan_bazli_400_doner()
    {
        var organizer = factory.CreateClientAs("organizator-1", "organizer");

        var response = await organizer.PostAsJsonAsync("/api/events",
            new { title = "", venueId = Guid.NewGuid(), startsAt = DateTimeOffset.UtcNow.AddDays(-1) },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestContext.Current.CancellationToken);
        Assert.Equal("Etkinlik adı gereklidir.", problem!.Errors["title"].Single());
        Assert.Equal("Etkinlik tarihi gelecekte olmalıdır.", problem.Errors["startsAt"].Single());
    }

    [Fact]
    public async Task Olmayan_etkinlik_404_doner()
    {
        var response = await factory.CreateClient().GetAsync($"/api/events/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);
        Assert.Equal("Etkinlik bulunamadı.", problem!.Title);
    }
}
