using MoozicOrb.Services.Interfaces;
using System.Collections.Concurrent;

namespace MoozicOrb.Services;

public class StreamSessionService : IStreamSessionService
{
    private readonly IRedisStreamStateService _redis;
    private readonly ConcurrentDictionary<string, (string StreamId, int UserId)> _connections
        = new();

    public StreamSessionService(IRedisStreamStateService redis)
    {
        _redis = redis;
    }

    public async Task RegisterConnectionAsync(
        string streamId,
        int userId,
        string connectionId,
        bool isBroadcaster)
    {
        _connections[connectionId] = (streamId, userId);

        if (isBroadcaster)
            await _redis.SetBroadcasterAsync(streamId, userId);
        else
            await _redis.AddListenerAsync(streamId, userId);
    }

    public async Task RemoveConnectionAsync(string streamId, int userId)
    {
        await _redis.RemoveListenerAsync(streamId, userId);
    }

    public async Task RemoveConnectionByConnectionIdAsync(string connectionId)
    {
        if (_connections.TryRemove(connectionId, out var info))
        {
            await _redis.RemoveListenerAsync(info.StreamId, info.UserId);
        }
    }

    public Task RefreshHeartbeatAsync(string streamId, int userId)
    {
        return _redis.RefreshTTLAsync(streamId);
    }
}


