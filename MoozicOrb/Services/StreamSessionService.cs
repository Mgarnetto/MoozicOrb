using System;
using System.Threading.Tasks;
using Moozicorb.Services.Interfaces;

namespace Moozicorb.Services
{
    public class StreamSessionService : IStreamSessionService
    {
        private readonly IRedisStreamStateService _redisStreamStateService;

        private static readonly TimeSpan StreamTtl = TimeSpan.FromSeconds(60);

        public StreamSessionService(IRedisStreamStateService redisStreamStateService)
        {
            _redisStreamStateService = redisStreamStateService;
        }

        public async Task StartStreamAsync(int streamId)
        {
            await _redisStreamStateService.SetStreamLiveAsync(streamId, StreamTtl);
        }

        public async Task EndStreamAsync(int streamId)
        {
            await _redisStreamStateService.EndStreamAsync(streamId);
        }

        public async Task JoinStreamAsync(int streamId, int userId)
        {
            await _redisStreamStateService.AddListenerAsync(streamId, userId);
            await _redisStreamStateService.TouchStreamAsync(streamId, StreamTtl);
        }

        public async Task LeaveStreamAsync(int streamId, int userId)
        {
            await _redisStreamStateService.RemoveListenerAsync(streamId, userId);
        }

        public async Task<bool> IsStreamLiveAsync(int streamId)
        {
            return await _redisStreamStateService.IsStreamLiveAsync(streamId);
        }

        public async Task<int> GetListenerCountAsync(int streamId)
        {
            return await _redisStreamStateService.GetListenerCountAsync(streamId);
        }
    }
}

