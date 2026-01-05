using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moozicorb.Services.Interfaces;

namespace Moozicorb.Services
{
    public class RedisStreamStateService : IRedisStreamStateService
    {
        private readonly IDatabase _redis;

        public RedisStreamStateService(IConnectionMultiplexer redis)
        {
            _redis = redis.GetDatabase();
        }

        private static string StreamKey(int streamId) => $"stream:{streamId}";
        private static string ListenerKey(int streamId) => $"stream:{streamId}:listeners";

        public async Task<bool> IsStreamLiveAsync(int streamId)
        {
            return await _redis.KeyExistsAsync(StreamKey(streamId));
        }

        public async Task SetStreamLiveAsync(int streamId, TimeSpan ttl)
        {
            await _redis.StringSetAsync(StreamKey(streamId), "live", ttl);
        }

        public async Task EndStreamAsync(int streamId)
        {
            await _redis.KeyDeleteAsync(new RedisKey[]
            {
                StreamKey(streamId),
                ListenerKey(streamId)
            });
        }

        public async Task AddListenerAsync(int streamId, int userId)
        {
            await _redis.SetAddAsync(ListenerKey(streamId), userId);
        }

        public async Task RemoveListenerAsync(int streamId, int userId)
        {
            await _redis.SetRemoveAsync(ListenerKey(streamId), userId);
        }

        public async Task<int> GetListenerCountAsync(int streamId)
        {
            return (int)await _redis.SetLengthAsync(ListenerKey(streamId));
        }

        public async Task<IEnumerable<int>> GetListenersAsync(int streamId)
        {
            var values = await _redis.SetMembersAsync(ListenerKey(streamId));
            return values.Select(v => (int)v);
        }

        public async Task TouchStreamAsync(int streamId, TimeSpan ttl)
        {
            await _redis.KeyExpireAsync(StreamKey(streamId), ttl);
            await _redis.KeyExpireAsync(ListenerKey(streamId), ttl);
        }
    }
}
