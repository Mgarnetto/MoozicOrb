using Microsoft.AspNetCore.SignalR;
using SIPSorcery.Net;
using System.Collections.Concurrent;
using MoozicOrb.Radio;
using System.Threading.Tasks;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace MoozicOrb.Hubs
{
    public class TestStreamHub : Hub
    {
        private static readonly ConcurrentDictionary<string, WebRtcAudioBridge> _activeBridges = new();
        private readonly IHubContext<TestStreamHub> _hubContext;

        // --- METERED.CA CONFIGURATION ---
        // Replace 'example' and 'example_secret_key' with your actual dashboard values
        private const string METERED_DOMAIN = "mo.metered.live";
        private const string METERED_SECRET_KEY = "gzOScPrYoQCOr9Vj_ubpRBSIYYINi-lC7kYEqnv-6VYm77js";

        public TestStreamHub(IHubContext<TestStreamHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task RequestOffer()
        {
            var connectionId = Context.ConnectionId;
            List<RTCIceServer> iceServers = new List<RTCIceServer>();

            // 1. Fetch TURN credentials from Metered.ca
            try
            {
                using (var httpClient = new HttpClient())
                {
                    // This API call gets us a list of STUN and TURN servers with temp credentials
                    var response = await httpClient.GetStringAsync($"https://{METERED_DOMAIN}.metered.live/api/v1/turn/credentials?apiKey={METERED_SECRET_KEY}");
                    var meteredServers = JsonSerializer.Deserialize<List<MeteredIceServer>>(response);

                    if (meteredServers != null)
                    {
                        foreach (var s in meteredServers)
                        {
                            iceServers.Add(new RTCIceServer
                            {
                                urls = s.Urls,
                                username = s.Username,
                                credential = s.Credential
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Hub] Metered API Error: {ex.Message}. Falling back to basic STUN.");
                iceServers.Add(new RTCIceServer { urls = "stun:stun.l.google.com:19302" });
            }

            // 2. Pass the iceServers to the bridge
            // Note: Ensure your Bridge constructor is updated to accept List<RTCIceServer>
            var bridge = new WebRtcAudioBridge(iceServers);
            _activeBridges[connectionId] = bridge;

            bridge.OnIceCandidateGenerated += async (candidate) => {
                try
                {
                    await _hubContext.Clients.Client(connectionId).SendAsync("RtcIceCandidate", new { candidate = candidate });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Hub] ICE Relay Error: {ex.Message}");
                }
            };

            var sdp = await bridge.GetOfferSdp();

            // 3. Send BOTH the SDP and the iceServers to the Client
            await Clients.Caller.SendAsync("RtcOffer", new { sdp = sdp, iceServers = iceServers });
        }

        public async Task ReceiveAnswer(string sdp)
        {
            if (_activeBridges.TryGetValue(Context.ConnectionId, out var bridge))
            {
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
                    bridge.PeerConnection.addIceCandidate(new RTCIceCandidateInit { candidate = candidate });
                }
            }
        }

        public override async Task OnDisconnectedAsync(Exception? ex)
        {
            if (_activeBridges.TryRemove(Context.ConnectionId, out var bridge))
            {
                bridge.Dispose();
            }
            await base.OnDisconnectedAsync(ex);
        }
    }

    // --- HELPER CLASS FOR DESERIALIZATION ---
    // This matches the JSON structure returned by Metered.ca
    public class MeteredIceServer
    {
        [JsonPropertyName("urls")]
        public string Urls { get; set; }

        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonPropertyName("credential")]
        public string Credential { get; set; }
    }
}