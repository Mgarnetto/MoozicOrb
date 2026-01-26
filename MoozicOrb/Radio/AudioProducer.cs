using System;
using System.Threading;
using System.Threading.Tasks;

namespace MoozicOrb.Radio
{
    /// <summary>
    /// Produces raw PCM audio frames (48kHz, 16-bit, mono).
/// Can generate sine wave, white noise, or read from file.
    /// </summary>
    public class AudioProducer : IDisposable
    {
        public event Action<short[]>? OnPcmFrame;

        private CancellationTokenSource? _cts;
        private const int SampleRate = 48000;
    private const int FrameSize = 960; // 20ms @ 48kHz

 /// <summary>
        /// Start producing audio frames (sine wave test tone).
    /// </summary>
   public void StartSineWave(double frequency = 440.0)
      {
      _cts = new CancellationTokenSource();
      Task.Run(() => SineWaveLoop(frequency, _cts.Token));
Console.WriteLine($"[AudioProducer] Started sine wave generator: {frequency}Hz");
      }

        private async Task SineWaveLoop(double frequency, CancellationToken token)
        {
            double phase = 0;
   double step = 2 * Math.PI * frequency / SampleRate;
     short[] buffer = new short[FrameSize];

        try
            {
  while (!token.IsCancellationRequested)
         {
        for (int i = 0; i < FrameSize; i++)
 {
      buffer[i] = (short)(Math.Sin(phase) * short.MaxValue * 0.5); // 0.5 to avoid clipping
      phase += step;
         if (phase > 2 * Math.PI)
    phase -= 2 * Math.PI;
  }

                OnPcmFrame?.Invoke(buffer);
   await Task.Delay(20, token); // 20ms per frame
     }
     }
          catch (OperationCanceledException)
            {
    Console.WriteLine("[AudioProducer] Sine wave loop stopped");
            }
        }

  /// <summary>
 /// Generate silence frames.
        /// </summary>
      public void StartSilence()
    {
   _cts = new CancellationTokenSource();
 Task.Run(() => SilenceLoop(_cts.Token));
    Console.WriteLine("[AudioProducer] Started silence generator");
        }

        private async Task SilenceLoop(CancellationToken token)
     {
       short[] silence = new short[FrameSize];

         try
            {
      while (!token.IsCancellationRequested)
                {
                    OnPcmFrame?.Invoke(silence);
        await Task.Delay(20, token);
      }
       }
   catch (OperationCanceledException)
            {
      Console.WriteLine("[AudioProducer] Silence loop stopped");
            }
 }

        public void Stop()
        {
       _cts?.Cancel();
            Console.WriteLine("[AudioProducer] Stopped");
  }

        public void Dispose()
        {
       Stop();
      _cts?.Dispose();
     }
}
}
