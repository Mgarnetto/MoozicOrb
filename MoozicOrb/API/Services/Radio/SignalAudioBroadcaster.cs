using Microsoft.AspNetCore.SignalR;
using MoozicOrb.Hubs;
using System.Threading.Tasks;

namespace MoozicOrb.Services.Radio
{
    public class SignalRAudioBroadcaster : IAudioBroadcaster
    {
        private readonly IHubContext<TestStreamHub> _hubContext;

        public SignalRAudioBroadcaster(IHubContext<TestStreamHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task BroadcastAudioChunkAsync(byte[] audioData)
        {
            // Pushes raw data to all connected clients listening on 'ReceiveAudio'
            // In a real scenario, you might group users to reduce load
            await _hubContext.Clients.All.SendAsync("ReceiveAudio", audioData);
        }
    }
}