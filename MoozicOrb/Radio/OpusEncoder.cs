using System;

namespace MoozicOrb.Radio
{
    /// <summary>
    /// Placeholder encoder. For now, we'll feed raw PCM to SIPSorcery 
    /// and let it handle codec negotiation/encoding.
    /// (Opus encoding can be added later if needed.)
    /// </summary>
    public class OpusEncoder : IDisposable
    {
        public OpusEncoder()
        {
            Console.WriteLine("[OpusEncoder] Initialized (passthrough mode)");
        }

        /// <summary>
        /// Return PCM frame as-is (SIPSorcery will encode based on negotiated codec).
        /// </summary>
        public short[] Passthrough(short[] pcmFrame)
        {
            return pcmFrame;
        }

        public void Dispose()
        {
            Console.WriteLine("[OpusEncoder] Disposed");
        }
    }
}
