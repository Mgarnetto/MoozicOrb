using StackExchange.Redis;
using MoozicOrb.Services.Interfaces;

namespace MoozicOrb.Services;

public class RedisStreamStateService : IRedisStreamStateService
{
    private readonly IDatabase _db;
    private static readonly TimeSpan TTL = TimeSpan.FromMinutes(1);

    public RedisStreamStateService(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    private static string Listeners(string streamId) => $"stream:{streamId}:listeners";
    private static string Broadcaster(string streamId) => $"stream:{streamId}:broadcaster";

    public async Task AddListenerAsync(string streamId, int userId)
    {
        await _db.SetAddAsync(Listeners(streamId), userId);
        await RefreshTTLAsync(streamId);
    }

    public async Task RemoveListenerAsync(string streamId, int userId)
    {
        await _db.SetRemoveAsync(Listeners(streamId), userId);
    }

    public Task<bool> IsListenerAsync(string streamId, int userId)
    {
        return _db.SetContainsAsync(Listeners(streamId), userId);
    }

    public async Task SetBroadcasterAsync(string streamId, int userId)
    {
        await _db.StringSetAsync(Broadcaster(streamId), userId, TTL);
    }

    public async Task<int?> GetBroadcasterAsync(string streamId)
    {
        var value = await _db.StringGetAsync(Broadcaster(streamId));
        return value.HasValue ? (int)value : null;
    }

    public async Task RefreshTTLAsync(string streamId)
    {
        await _db.KeyExpireAsync(Listeners(streamId), TTL);
        await _db.KeyExpireAsync(Broadcaster(streamId), TTL);
    }

    public async Task<long> GetListenerCountAsync(string streamId)
    {
        return await _db.SetLengthAsync(Listeners(streamId));
    }

}

