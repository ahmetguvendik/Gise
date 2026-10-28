using Gise.Reservation.Application.Interfaces;
using StackExchange.Redis;

namespace Gise.Reservation.Infrastructure.Locking;

// Koltuk başına "hold:{etkinlik}:{koltuk}" anahtarı, değeri tutmanın ID'si (bkz. tasarım 5). Kullanıcı başına
// "hold-user:{etkinlik}:{kullanıcı}" anahtarı aynı etkinlikte ikinci tutmayı engeller. Hepsi aynı süreyle yazılır.
public sealed class RedisSeatLockService : ISeatLockService
{
    // Kontrol ve yazma tek betikte: Redis betiği atomik çalıştırır, araya başka istek giremez.
    // Bir koltuk bile başka bir tutmadaysa hiçbiri yazılmaz. Değeri bu tutmanın ID'si olan anahtar engel sayılmaz:
    // komut yeniden geldiğinde aynı tutma kilidini tekrar alır.
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

    // Yalnızca değeri bu tutmanın ID'si olan anahtarlar silinir: süre dolup başkası tutmuşsa onun kilidi kalır.
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

    public async Task<SeatLockResult> TryLockAsync(Guid eventId, string userId, IReadOnlyCollection<Guid> seatIds, Guid holdId, TimeSpan duration)
    {
        var result = (int)await _redis.GetDatabase().ScriptEvaluateAsync(
            LockScript,
            [UserKey(eventId, userId), .. seatIds.Select(seatId => SeatKey(eventId, seatId))],
            [holdId.ToString(), (long)duration.TotalMilliseconds]);

        return result switch
        {
            0 => SeatLockResult.Locked,
            2 => SeatLockResult.UserAlreadyHolding,
            _ => SeatLockResult.SeatTaken
        };
    }

    public Task ReleaseAsync(Guid eventId, string userId, IReadOnlyCollection<Guid> seatIds, Guid holdId)
        => _redis.GetDatabase().ScriptEvaluateAsync(
            ReleaseScript,
            [UserKey(eventId, userId), .. seatIds.Select(seatId => SeatKey(eventId, seatId))],
            [holdId.ToString()]);

    public async Task<IReadOnlySet<Guid>> GetLockedSeatIdsAsync(Guid eventId, IReadOnlyCollection<Guid> seatIds)
    {
        if (seatIds.Count == 0)
            return new HashSet<Guid>();

        var ids = seatIds.ToList();
        var values = await _redis.GetDatabase().StringGetAsync(ids.Select(seatId => SeatKey(eventId, seatId)).ToArray());

        return ids.Where((_, i) => values[i].HasValue).ToHashSet();
    }

    private static RedisKey SeatKey(Guid eventId, Guid seatId) => $"hold:{eventId}:{seatId}";

    private static RedisKey UserKey(Guid eventId, string userId) => $"hold-user:{eventId}:{userId}";
}
