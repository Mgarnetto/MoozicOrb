using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SIPSorcery.Media;
using SIPSorcery.Net;
using SIPSorceryMedia.Abstractions;

namespace MoozicOrb.Radio
{
    public class WebRtcAudioBridge : IDisposable
    {
        public RTCPeerConnection PeerConnection { get; private set; }
        private AudioExtrasSource _audioSource;
        private bool _isStarted = false;

        // NEW: Event to bubble up ICE candidates to your SignalR Hub
        public event Action<string> OnIceCandidateGenerated;

        public WebRtcAudioBridge()
        {
            // STUN: Essential for shared servers to discover public IPs
            var config = new RTCConfiguration
            {
                iceServers = new List<RTCIceServer> {
                    new RTCIceServer { urls = "stun:stun.l.google.com:19302" }
                }
            };

            PeerConnection = new RTCPeerConnection(config);

            // 1. Initialize Source with SineWave
            var audioEncoder = new AudioEncoder();
            _audioSource = new AudioExtrasSource(audioEncoder, new AudioSourceOptions
            {
                AudioSource = AudioSourcesEnum.SineWave
            });

            // Restrict to PCMU (8000Hz) - the most compatible server codec
            _audioSource.RestrictFormats(f => f.Codec == AudioCodecsEnum.PCMU);

            var audioTrack = new MediaStreamTrack(_audioSource.GetAudioSourceFormats(), MediaStreamStatusEnum.SendOnly);
            PeerConnection.addTrack(audioTrack);

            // 2. Wire up samples
            _audioSource.OnAudioSourceEncodedSample += PeerConnection.SendAudio;

            // 3. Handle Negotiation
            PeerConnection.OnAudioFormatsNegotiated += (formats) => {
                var format = formats.FirstOrDefault();
                if (!format.IsEmpty())
                {
                    _audioSource.SetAudioSourceFormat(format);
                }
            };

            // NEW: Handle ICE candidate generation (Trickle ICE)
            PeerConnection.onicecandidate += (candidate) => {
                if (candidate != null && !string.IsNullOrEmpty(candidate.candidate))
                {
                    OnIceCandidateGenerated?.Invoke(candidate.candidate);
                }
            };

            // 4. Manage Connection Lifecycle
            PeerConnection.onconnectionstatechange += async (state) => {
                Console.WriteLine($"[WebRtc] Connection State: {state}");

                if (state == RTCPeerConnectionState.connected && !_isStarted)
                {
                    _isStarted = true;
                    await _audioSource.StartAudio();
                }
                else if (state == RTCPeerConnectionState.failed || state == RTCPeerConnectionState.closed)
                {
                    _audioSource.Close();
                }
            };
        }

        public async Task<string> GetOfferSdp()
        {
            var offer = PeerConnection.createOffer();

            // Aggressive SCTP/DataChannel stripping to prevent server crashes
            string[] lines = offer.sdp.Split('\n');
            var cleanSdp = new List<string>();
            bool inApplicationSection = false;

            foreach (var line in lines)
            {
                if (line.StartsWith("m=application")) inApplicationSection = true;
                if (line.StartsWith("m=audio") || line.StartsWith("m=video")) inApplicationSection = false;

                if (!inApplicationSection && !line.Contains("sctp") && !line.Contains("SCTP"))
                {
                    cleanSdp.Add(line);
                }
            }

            offer.sdp = string.Join("\n", cleanSdp);
            await PeerConnection.setLocalDescription(offer);
            return offer.sdp;
        }

        public void Dispose()
        {
            _audioSource?.Close();
            PeerConnection?.Dispose();
        }
    }
}