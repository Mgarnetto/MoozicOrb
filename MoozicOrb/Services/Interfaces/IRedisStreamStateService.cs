using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Moozicorb.Services.Interfaces
{
    public interface IRedisStreamStateService
    {
        Task<bool> IsStreamLiveAsync(int streamId);

        Task SetStreamLiveAsync(int streamId, TimeSpan ttl);

        Task EndStreamAsync(int streamId);

        Task AddListenerAsync(int streamId, int userId);

        Task RemoveListenerAsync(int streamId, int userId);

        Task<int> GetListenerCountAsync(int streamId);

        Task<IEnumerable<int>> GetListenersAsync(int streamId);

        Task TouchStreamAsync(int streamId, TimeSpan ttl);
    }
}
