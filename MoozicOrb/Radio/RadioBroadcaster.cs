using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MoozicOrb.Radio
{
    public class RadioBroadcaster : IDisposable
    {
        public event Action<short[]>? OnPcmFrame;

        private CancellationTokenSource? _cts;
        private const int SampleRate = 48000;
        private const int FrameSize = 960; // 20ms @ 48kHz

        public void Start()
        {
            _cts = new CancellationTokenSource();
            Task.Run(() => BroadcastLoop(_cts.Token));
        }

        private async Task BroadcastLoop(CancellationToken token)
        {
            double frequency = 440.0;
            double phase = 0;
            double increment = 2 * Math.PI * frequency / SampleRate;
            short[] pcm = new short[FrameSize];

            while (!token.IsCancellationRequested)
            {
                for (int i = 0; i < FrameSize; i++)
                {
                    pcm[i] = (short)(Math.Sin(phase) * short.MaxValue);
                    phase += increment;
                    if (phase > 2 * Math.PI) phase -= 2 * Math.PI;
                }

                OnPcmFrame?.Invoke(pcm);
                await Task.Delay(20, token);
            }
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}
