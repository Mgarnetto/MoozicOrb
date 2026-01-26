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

        public event Action<string> OnIceCandidateGenerated;

        // FIXED: Added 'List<RTCIceServer> iceServers' parameter to match your Hub's call
        public WebRtcAudioBridge(List<RTCIceServer> iceServers)
        {
            // Use the iceServers passed in from the Hub (Metered.ca + fallback)
            var config = new RTCConfiguration
            {
                iceServers = iceServers
            };

            // Use named parameter to avoid constructor ambiguity
            PeerConnection = new RTCPeerConnection(configuration: config);

            var audioEncoder = new AudioEncoder();
            _audioSource = new AudioExtrasSource(audioEncoder, new AudioSourceOptions
            {
                AudioSource = AudioSourcesEnum.SineWave
            });

            _audioSource.RestrictFormats(f => f.Codec == AudioCodecsEnum.PCMU);

            var audioTrack = new MediaStreamTrack(_audioSource.GetAudioSourceFormats(), MediaStreamStatusEnum.SendOnly);
            PeerConnection.addTrack(audioTrack);

            _audioSource.OnAudioSourceEncodedSample += PeerConnection.SendAudio;

            PeerConnection.OnAudioFormatsNegotiated += (formats) => {
                var format = formats.FirstOrDefault();
                if (!format.IsEmpty())
                {
                    _audioSource.SetAudioSourceFormat(format);
                }
            };

            PeerConnection.onicecandidate += (candidate) => {
                if (candidate != null && !string.IsNullOrEmpty(candidate.candidate))
                {
                    OnIceCandidateGenerated?.Invoke(candidate.candidate);
                }
            };

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