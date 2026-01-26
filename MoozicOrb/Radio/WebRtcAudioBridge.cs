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

        public WebRtcAudioBridge()
        {
            var config = new RTCConfiguration
            {
                iceServers = new List<RTCIceServer> { new RTCIceServer { urls = "stun:stun.l.google.com:19302" } }
            };

            PeerConnection = new RTCPeerConnection(config);

            // 1. Initialize Source with an Explicit Encoder (Required in v10)
            var audioEncoder = new AudioEncoder();
            _audioSource = new AudioExtrasSource(audioEncoder, new AudioSourceOptions
            {
                AudioSource = AudioSourcesEnum.SineWave
            });

            // Restrict to PCMU (8000Hz) to ensure the SineWave generator is happy
            _audioSource.RestrictFormats(f => f.Codec == AudioCodecsEnum.PCMU);

            var audioTrack = new MediaStreamTrack(_audioSource.GetAudioSourceFormats(), MediaStreamStatusEnum.SendOnly);
            PeerConnection.addTrack(audioTrack);

            // 2. Wire up the encoded samples to the PeerConnection
            _audioSource.OnAudioSourceEncodedSample += PeerConnection.SendAudio;

            // 3. Handle Negotiation (Struct-Safe Check)
            PeerConnection.OnAudioFormatsNegotiated += (formats) => {
                var format = formats.FirstOrDefault();
                // AudioFormat is a struct in v10, use .IsEmpty() instead of != null
                if (!format.IsEmpty())
                {
                    _audioSource.SetAudioSourceFormat(format);
                }
            };

            // 4. Manage Connection Lifecycle
            PeerConnection.onconnectionstatechange += async (state) => {
                Console.WriteLine($"[WebRtc] Connection State: {state}");

                if (state == RTCPeerConnectionState.connected && !_isStarted)
                {
                    _isStarted = true;
                    // StartAudio is a Task in v10 - must be awaited or handled
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

            // Hard-strip SCTP/DataChannels to prevent the 'rtcsctprecv' thread crash
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
            // In v10, media sources use Close() instead of Dispose()
            _audioSource?.Close();
            PeerConnection?.Dispose();
        }
    }
}