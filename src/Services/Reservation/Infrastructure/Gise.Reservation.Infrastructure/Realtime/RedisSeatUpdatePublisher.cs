using System.Text.Json;
using Gise.Contracts.Realtime;
using Gise.Reservation.Application.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Gise.Reservation.Infrastructure.Realtime;

// Koltuk kilitleriyle aynı Redis bağlantısı; kanal SeatsChanged.Channel, gövde JSON.
public sealed class RedisSeatUpdatePublisher : ISeatUpdatePublisher
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisSeatUpdatePublisher> _logger;

    public RedisSeatUpdatePublisher(IConnectionMultiplexer redis, ILogger<RedisSeatUpdatePublisher> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task PublishAsync(Guid eventId, IReadOnlyCollection<Guid> seatIds, SeatState state)
    {
        if (seatIds.Count == 0)
            return;

        var message = new SeatsChanged { EventId = eventId, State = state, SeatIds = [.. seatIds] };
        try
        {
            await _redis.GetSubscriber().PublishAsync(
                RedisChannel.Literal(SeatsChanged.Channel), JsonSerializer.Serialize(message, JsonSerializerOptions.Web));
        }
        catch (Exception ex) when (ex is RedisException or TimeoutException)
        {
            _logger.LogWarning(ex, "Koltuk haritasına yayın yapılamadı: etkinlik {EventId}, {Count} koltuk {State}.", eventId, seatIds.Count, state);
        }
    }
}
