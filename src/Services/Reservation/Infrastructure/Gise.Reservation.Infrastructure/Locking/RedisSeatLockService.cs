using Gise.Reservation.Application.Interfaces;
using StackExchange.Redis;

namespace Gise.Reservation.Infrastructure.Locking;

// Koltuk başına "hold:{etkinlik}:{koltuk}" anahtarı, değeri siparişin ID'si (bkz. tasarım 5). Müşteri başına
// "hold-user:{etkinlik}:{müşteri}" anahtarı aynı etkinlikte ikinci siparişi engeller. Hepsi aynı süreyle yazılır.
public sealed class RedisSeatLockService : ISeatLockService
{
    // Kontrol ve yazma tek betikte: Redis betiği atomik çalıştırır, araya başka istek giremez.
    // Bir koltuk bile başka bir siparişteyse hiçbiri yazılmaz. Değeri bu siparişin ID'si olan anahtar engel sayılmaz:
    // komut yeniden geldiğinde aynı sipariş kilidini tekrar alır.
    private const string LockScript = @"
        local owner = redis.call('GET', KEYS[1])
        if owner and owner ~= ARGV[1] then return 2 end
        for i = 2, #KEYS do
            local holder = redis.call('GET', KEYS[i])
            if holder and holder ~= ARGV[1] then return 1 end
        end
        for i = 1, #KEYS do
            redis.call('SET', KEYS[i], ARGV[1], 'PX', ARGV[2])
        end
        return 0";

    // Satıştan önce: yalnızca koltuk anahtarlarına bakılır. Boş ya da bu siparişte olanlar yeniden bu sipariş adına yazılır.
    private const string ClaimScript = @"
        for i = 1, #KEYS do
            local holder = redis.call('GET', KEYS[i])
            if holder and holder ~= ARGV[1] then return 1 end
        end
        for i = 1, #KEYS do
            redis.call('SET', KEYS[i], ARGV[1], 'PX', ARGV[2])
        end
        return 0";

    // Yalnızca değeri bu siparişin ID'si olan anahtarlar silinir: süre dolup başkası tutmuşsa onun kilidi kalır.
    private const string ReleaseScript = @"
        for i = 1, #KEYS do
            if redis.call('GET', KEYS[i]) == ARGV[1] then redis.call('DEL', KEYS[i]) end
        end
        return 0";

    private readonly IConnectionMultiplexer _redis;

    public RedisSeatLockService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<SeatLockResult> TryLockAsync(Guid eventId, string customerId, IReadOnlyCollection<Guid> seatIds, Guid orderId, TimeSpan duration)
    {
        var result = (int)await _redis.GetDatabase().ScriptEvaluateAsync(
            LockScript,
            [CustomerKey(eventId, customerId), .. seatIds.Select(seatId => SeatKey(eventId, seatId))],
            [orderId.ToString(), (long)duration.TotalMilliseconds]);

        return result switch
        {
            0 => SeatLockResult.Locked,
            2 => SeatLockResult.UserAlreadyHolding,
            _ => SeatLockResult.SeatTaken
        };
    }

    public async Task<bool> TryClaimAsync(Guid eventId, IReadOnlyCollection<Guid> seatIds, Guid orderId, TimeSpan duration)
        => (int)await _redis.GetDatabase().ScriptEvaluateAsync(
            ClaimScript,
            seatIds.Select(seatId => SeatKey(eventId, seatId)).ToArray(),
            [orderId.ToString(), (long)duration.TotalMilliseconds]) == 0;

    public Task ReleaseAsync(Guid eventId, string customerId, IReadOnlyCollection<Guid> seatIds, Guid orderId)
        => _redis.GetDatabase().ScriptEvaluateAsync(
            ReleaseScript,
            [CustomerKey(eventId, customerId), .. seatIds.Select(seatId => SeatKey(eventId, seatId))],
            [orderId.ToString()]);

    public async Task<IReadOnlySet<Guid>> GetLockedSeatIdsAsync(Guid eventId, IReadOnlyCollection<Guid> seatIds)
    {
        if (seatIds.Count == 0)
            return new HashSet<Guid>();

        var ids = seatIds.ToList();
        var values = await _redis.GetDatabase().StringGetAsync(ids.Select(seatId => SeatKey(eventId, seatId)).ToArray());

        return ids.Where((_, i) => values[i].HasValue).ToHashSet();
    }

    private static RedisKey SeatKey(Guid eventId, Guid seatId) => $"hold:{eventId}:{seatId}";

    private static RedisKey CustomerKey(Guid eventId, string customerId) => $"hold-user:{eventId}:{customerId}";
}
