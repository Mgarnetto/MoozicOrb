using Microsoft.AspNetCore.SignalR;
using SIPSorcery.Net;
using System.Collections.Concurrent;
using MoozicOrb.Radio;
using System.Threading.Tasks;
using System;

namespace MoozicOrb.Hubs
{
    public class TestStreamHub : Hub
    {
        // Static dictionary to persist bridge instances across transient hub calls
        private static readonly ConcurrentDictionary<string, WebRtcAudioBridge> _activeBridges = new();

        // IHubContext allows us to communicate with clients even after this Hub instance is disposed
        private readonly IHubContext<TestStreamHub> _hubContext;

        public TestStreamHub(IHubContext<TestStreamHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task RequestOffer()
        {
            var connectionId = Context.ConnectionId;
            var bridge = new WebRtcAudioBridge();
            _activeBridges[connectionId] = bridge;

            bridge.PeerConnection.onicecandidate += async (candidate) => {
                if (!string.IsNullOrEmpty(candidate?.candidate))
                {
                    await _hubContext.Clients.Client(connectionId).SendAsync("RtcIceCandidate", new { candidate = candidate.candidate });
                }
            };

            // Use the v10 helper method
            var sdp = await bridge.GetOfferSdp();
            await Clients.Caller.SendAsync("RtcOffer", new { sdp = sdp });
        }

        public async Task ReceiveAnswer(string sdp)
        {
            if (_activeBridges.TryGetValue(Context.ConnectionId, out var bridge))
            {
                // SIPSorcery's setRemoteDescription is internaly async but follows the task pattern
                bridge.PeerConnection.setRemoteDescription(new RTCSessionDescriptionInit
                {
                    type = RTCSdpType.answer,
                    sdp = sdp
                });
            }
            await Task.CompletedTask;
        }

        public void ReceiveIceCandidate(string candidate)
        {
            if (_activeBridges.TryGetValue(Context.ConnectionId, out var bridge))
            {
                if (!string.IsNullOrWhiteSpace(candidate))
                {
                    // Add the client's ICE candidate to the server's PeerConnection
                    bridge.PeerConnection.addIceCandidate(new RTCIceCandidateInit { candidate = candidate });
                }
            }
        }

        public override async Task OnDisconnectedAsync(Exception? ex)
        {
            // Crucial: Dispose the bridge when the client leaves to stop the audio loop and free ports
            if (_activeBridges.TryRemove(Context.ConnectionId, out var bridge))
            {
                bridge.Dispose();
            }
            await base.OnDisconnectedAsync(ex);
        }
    }
}