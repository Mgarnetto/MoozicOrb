using MoozicOrb.API.Models;

namespace MoozicOrb.API.Services.Interfaces
{
    public interface IStreamApiService
    {
        StreamInfoDto GetStreamInfo(long trackId);
    }
}
