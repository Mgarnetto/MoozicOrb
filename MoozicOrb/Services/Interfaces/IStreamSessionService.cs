using System.Threading.Tasks;

namespace Moozicorb.Services.Interfaces
{
    public interface IStreamSessionService
    {
        Task StartStreamAsync(int streamId);

        Task EndStreamAsync(int streamId);

        Task JoinStreamAsync(int streamId, int userId);

        Task LeaveStreamAsync(int streamId, int userId);

        Task<bool> IsStreamLiveAsync(int streamId);

        Task<int> GetListenerCountAsync(int streamId);
    }
}
