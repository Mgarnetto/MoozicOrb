using MoozicOrb.Api.Models;
using MoozicOrb.Api.Services.Interfaces;
using MoozicOrb.IO;

namespace MoozicOrb.Api.Services
{
    public class StreamApiService : IStreamApiService
    {
        public StreamInfoDto GetStreamInfo(long trackId)
        {
            // Use the IO class we created earlier
            var io = new GetStreamInfo();
            return io.Get(trackId);
        }
    }
}