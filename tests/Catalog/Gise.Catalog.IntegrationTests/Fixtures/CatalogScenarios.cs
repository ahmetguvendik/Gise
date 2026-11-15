using System.Net;
using System.Net.Http.Json;
using Gise.Catalog.Application.Features.Commands.Event.Response;
using Gise.Catalog.Application.Features.Commands.Venue.Response;
using Gise.Catalog.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gise.Catalog.IntegrationTests.Fixtures;

// Önbellek testlerinin ortak senaryosu; gerçek uçlardan ilerler. Bölüm kimliği doğrudan veritabanından okunur: mekân
// detayını okumak mekân önbelleğini daha testin başında doldururdu.
public static class CatalogScenarios
{
    public static async Task<(Guid VenueId, Guid SectionId)> CreateVenueAsync(CatalogApiFactory factory, string name = "Önbellek Sahnesi")
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await factory.CreateClientAs("admin-1", "admin").PostAsJsonAsync("/api/venues", new
        {
            name,
            city = "Ankara",
            sections = new[] { new { name = "Salon", rows = new[] { new { row = "A", seatCount = 4 } } } }
        }, ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var venueId = (await response.Content.ReadFromJsonAsync<CreateVenueResponse>(ct))!.Id;

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var sectionId = await db.Sections.Where(s => s.VenueId == venueId).Select(s => s.Id).SingleAsync(ct);
        return (venueId, sectionId);
    }

    // Taslak oluşturur (organizatör); fiyatları ve yayını gerekirse çağıran ekler.
    public static async Task<Guid> CreateDraftEventAsync(CatalogApiFactory factory, Guid venueId, string title = "Önbellek Konseri")
    {
        var ct = TestContext.Current.CancellationToken;
        var created = await factory.CreateClientAs("organizator-1", "organizer").PostAsJsonAsync("/api/events",
            new { title, venueId, startsAt = DateTimeOffset.UtcNow.AddDays(30) }, ct);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        return (await created.Content.ReadFromJsonAsync<CreateEventResponse>(ct))!.Id;
    }

    public static async Task<(Guid EventId, Guid VenueId, Guid SectionId)> CreatePublishedEventAsync(
        CatalogApiFactory factory, string title = "Önbellek Konseri", decimal price = 100m)
    {
        var ct = TestContext.Current.CancellationToken;
        var (venueId, sectionId) = await CreateVenueAsync(factory);
        var eventId = await CreateDraftEventAsync(factory, venueId, title);

        var prices = await factory.CreateClientAs("organizator-1", "organizer")
            .PutAsJsonAsync($"/api/events/{eventId}/prices", new { prices = new[] { new { sectionId, price } } }, ct);
        Assert.Equal(HttpStatusCode.OK, prices.StatusCode);

        var published = await factory.CreateClientAs("admin-1", "admin").PostAsync($"/api/events/{eventId}/publish", content: null, ct);
        Assert.Equal(HttpStatusCode.OK, published.StatusCode);

        return (eventId, venueId, sectionId);
    }
}
