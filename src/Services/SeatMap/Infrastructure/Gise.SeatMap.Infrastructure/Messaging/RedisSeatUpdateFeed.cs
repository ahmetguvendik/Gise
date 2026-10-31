using System.Text.Json;
using Gise.Contracts.Realtime;
using Gise.SeatMap.Application.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Gise.SeatMap.Infrastructure.Messaging;

// Redis kanalı SeatsChanged.Channel. Her seatmap-api kopyası kendisi abone olur ve mesajı yalnızca kendi bağlantılarına
// iletir; kanal mesajı bütün kopyalara dağıttığı için SignalR backplane'i gerekmez. Bağlantı koparsa StackExchange.Redis
// yeniden bağlanıp aboneliği yeniler; aradaki mesajlar kaybolur (tarayıcı haritayı yeniden yükleyince düzelir).
public sealed class RedisSeatUpdateFeed : ISeatUpdateFeed
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisSeatUpdateFeed> _logger;

    public RedisSeatUpdateFeed(IConnectionMultiplexer redis, ILogger<RedisSeatUpdateFeed> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task SubscribeAsync(Func<SeatsChanged, Task> onMessage, CancellationToken cancellationToken)
    {
        var channel = await _redis.GetSubscriber().SubscribeAsync(RedisChannel.Literal(SeatsChanged.Channel));
        channel.OnMessage(async message =>
        {
            SeatsChanged? change;
            try
            {
                change = JsonSerializer.Deserialize<SeatsChanged>(message.Message.ToString(), JsonSerializerOptions.Web);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Koltuk değişikliği okunamadı, atlandı.");
                return;
            }

            if (change is not null)
                await onMessage(change);
        });

        try
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            await channel.UnsubscribeAsync();
        }
    }
}
