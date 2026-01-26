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

            // Use our event to bubble up candidates
            bridge.OnIceCandidateGenerated += async (candidate) => {
                try
                {
                    // Only send if the Hub connection is still alive
                    await _hubContext.Clients.Client(connectionId).SendAsync("RtcIceCandidate", new { candidate = candidate });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Hub] ICE Relay Error: {ex.Message}");
                }
            };

            var sdp = await bridge.GetOfferSdp();
            await Clients.Caller.SendAsync("RtcOffer", new { sdp = sdp });
        }

        public async Task ReceiveAnswer(string sdp)
        {
            if (_activeBridges.TryGetValue(Context.ConnectionId, out var bridge))
            {
                // FIX: Remove 'await'. SIPSorcery v10 returns void here.
                bridge.PeerConnection.setRemoteDescription(new RTCSessionDescriptionInit
                {
                    type = RTCSdpType.answer,
                    sdp = sdp
                });
            }
            await Task.CompletedTask; // Keep the method signature happy
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