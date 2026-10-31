using Gise.SeatMap.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Gise.SeatMap.Api.Hubs;

// Reservation'ın Redis yayınlarını o etkinliği izleyen tarayıcılara iletir. Kafka ya da RabbitMQ değil, kalıcı olmayan bir
// Redis kanalı dinlenir; anlık iletim de bağlantıların durduğu bu process'te yapılmak zorunda. Mesajlaşma kuralının
// (dinleyiciler worker'larda) bilinçli istisnası (bkz. tasarım 3.11).
public sealed class SeatUpdatesRelay : BackgroundService
{
    private readonly ISeatUpdateFeed _feed;
    private readonly IHubContext<SeatMapHub> _hub;

    public SeatUpdatesRelay(ISeatUpdateFeed feed, IHubContext<SeatMapHub> hub)
    {
        _feed = feed;
        _hub = hub;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
        => _feed.SubscribeAsync(
            change => _hub.Clients.Group(SeatMapHub.GroupOf(change.EventId)).SendAsync(SeatMapHub.SeatsChangedMethod, change, stoppingToken),
            stoppingToken);
}
