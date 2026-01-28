using MoozicOrb.Api.Models;

namespace MoozicOrb.Api.Services.Interfaces
{
    public interface IStreamApiService
    {
        StreamInfoDto GetStreamInfo(long trackId);
    }
}
