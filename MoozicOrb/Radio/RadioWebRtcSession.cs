using System;
using System.Linq;
using Concentus.Enums;
using Concentus.Structs;
using SIPSorcery.Net;
using SIPSorcery.Media;

namespace MoozicOrb.Radio
{
    public class RadioWebRtcSession
    {
        public RTCPeerConnection PeerConnection { get; }

        private readonly OpusEncoder _opusEncoder;
        private readonly byte[] _opusBuffer = new byte[4000];

        public RadioWebRtcSession(RadioBroadcaster broadcaster)
        {
            PeerConnection = new RTCPeerConnection();

            // Create a Concentus Opus encoder for 48 kHz mono
            _opusEncoder = new OpusEncoder(48000, 1, OpusApplication.OPUS_APPLICATION_AUDIO);

            // Whenever broadcaster gives PCM, encode then send
            broadcaster.OnPcmFrame += pcm =>
            {
                // Encode to Opus
                int length = _opusEncoder.Encode(pcm, 0, pcm.Length, _opusBuffer, 0, _opusBuffer.Length);

                if (length > 0)
                {
                    byte[] opusFrame = new byte[length];
                    Buffer.BlockCopy(_opusBuffer, 0, opusFrame, 0, length);

                    // Send the encoded Opus frame as RTP
                    PeerConnection.SendAudio(960, opusFrame);
                }
            };
        }

        public string CreateOffer()
        {
            var offer = PeerConnection.createOffer();
            PeerConnection.setLocalDescription(offer);
            return offer.sdp;
        }

        public void SetAnswer(string sdp)
        {
            PeerConnection.setRemoteDescription(new RTCSessionDescriptionInit
            {
                type = RTCSdpType.answer,
                sdp = sdp
            });
        }
    }
}

