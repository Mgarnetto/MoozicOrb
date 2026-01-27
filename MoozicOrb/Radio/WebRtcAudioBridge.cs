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

        public WebRtcAudioBridge(List<RTCIceServer> iceServers)
        {
            // Simple config: use what we're given, allow all connection types
            var config = new RTCConfiguration
            {
                iceServers = iceServers,
                iceTransportPolicy = RTCIceTransportPolicy.all
            };

            PeerConnection = new RTCPeerConnection(configuration: config);

            _audioSource = new AudioExtrasSource(new AudioEncoder(), new AudioSourceOptions
            {
                AudioSource = AudioSourcesEnum.SineWave
            });

            _audioSource.RestrictFormats(f => f.Codec == AudioCodecsEnum.PCMU);

            var audioTrack = new MediaStreamTrack(_audioSource.GetAudioSourceFormats(), MediaStreamStatusEnum.SendOnly);
            PeerConnection.addTrack(audioTrack);

            _audioSource.OnAudioSourceEncodedSample += PeerConnection.SendAudio;

            PeerConnection.OnAudioFormatsNegotiated += (formats) => {
                var format = formats.FirstOrDefault();
                if (!format.IsEmpty()) _audioSource.SetAudioSourceFormat(format);
            };

            PeerConnection.onicecandidate += (candidate) => {
                if (candidate != null && !string.IsNullOrEmpty(candidate.candidate))
                    OnIceCandidateGenerated?.Invoke(candidate.candidate);
            };

            PeerConnection.onconnectionstatechange += async (state) => {
                Console.WriteLine($"[WebRtc] State: {state}");
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
            // Basic SDP cleanup to remove SCTP (Data Channels) which can cause issues
            string[] lines = offer.sdp.Split('\n');
            var cleanSdp = lines.Where(l => !l.Contains("sctp") && !l.Contains("SCTP")).ToList();
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