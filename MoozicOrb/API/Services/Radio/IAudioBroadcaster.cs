using System.Threading.Tasks;

namespace MoozicOrb.Services.Radio
{
    // The "Plug" - Implement this for SignalR today, WebRTC tomorrow.
    public interface IAudioBroadcaster
    {
        // Broadcasts a generic audio packet (format defined by the station)
        Task BroadcastAudioChunkAsync(byte[] audioData);
    }
}
