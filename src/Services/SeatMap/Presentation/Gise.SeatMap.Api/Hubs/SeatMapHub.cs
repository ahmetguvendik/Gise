using Microsoft.AspNetCore.SignalR;

namespace Gise.SeatMap.Api.Hubs;

// Koltuk haritası (bkz. tasarım 3.11): tarayıcı etkinliği izlemeye başlar (Watch), koltuk değişiklikleri "SeatsChanged"
// olarak gelir. Giriş gerekmez; harita zaten herkese açık. Bağlantı başına izlenebilecek etkinlik sınırlı: bir sayfa tek
// etkinliğe bakar.
public sealed class SeatMapHub : Hub
{
    public const string SeatsChangedMethod = "SeatsChanged";
    private const int MaxWatchedEvents = 3;

    public async Task Watch(Guid eventId)
    {
        var watched = Watched();
        if (!watched.Contains(eventId))
        {
            if (watched.Count >= MaxWatchedEvents)
                throw new HubException($"Bir bağlantı en fazla {MaxWatchedEvents} etkinliği izleyebilir.");
            watched.Add(eventId);
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupOf(eventId));
    }

    public async Task Unwatch(Guid eventId)
    {
        Watched().Remove(eventId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupOf(eventId));
    }

    public static string GroupOf(Guid eventId) => $"event:{eventId}";

    // Bağlantının izlediği etkinlikler; bağlantı kapanınca SignalR grupları kendisi temizler.
    private HashSet<Guid> Watched()
    {
        if (Context.Items.TryGetValue(nameof(Watched), out var value) && value is HashSet<Guid> set)
            return set;

        var created = new HashSet<Guid>();
        Context.Items[nameof(Watched)] = created;
        return created;
    }
}
