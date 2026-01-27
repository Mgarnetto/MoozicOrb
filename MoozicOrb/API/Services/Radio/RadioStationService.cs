using NAudio.Wave;
using NAudio.Wave.SampleProviders; // Required for stereo conversion
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace MoozicOrb.Services.Radio
{
    public class RadioStationService : BackgroundService
    {
        private readonly IAudioBroadcaster _broadcaster;
        private readonly string _musicFolder;

        // CONFIG: CD Quality (44.1kHz, 16-bit, Stereo)
        private readonly WaveFormat _broadcastFormat = new WaveFormat(44100, 16, 2);

        public RadioStationService(IAudioBroadcaster broadcaster, IWebHostEnvironment env)
        {
            _broadcaster = broadcaster;
            _musicFolder = Path.Combine(env.WebRootPath, "music");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Directory.CreateDirectory(_musicFolder);

            while (!stoppingToken.IsCancellationRequested)
            {
                var files = Directory.GetFiles(_musicFolder, "*.mp3");
                if (files.Length == 0) { await Task.Delay(5000, stoppingToken); continue; }

                foreach (var file in files)
                {
                    if (stoppingToken.IsCancellationRequested) break;
                    Console.WriteLine($"[Radio] Now Playing (Hi-Fi): {Path.GetFileName(file)}");
                    await StreamFileAsync(file, stoppingToken);
                }
            }
        }

        private async Task StreamFileAsync(string filePath, CancellationToken token)
        {
            try
            {
                using var reader = new AudioFileReader(filePath);

                // RESAMPLING: Force everything to 44.1kHz Stereo
                using var resampler = new MediaFoundationResampler(reader, _broadcastFormat);
                resampler.ResamplerQuality = 60; // Max quality

                // BUFFER CALCULATION:
                // 44100 Hz * 2 channels * 2 bytes = 176,400 bytes/second
                // SignalR default message limit is ~32KB.
                // We will send ~100ms chunks = 17,640 bytes. This is safe.
                int bufferSize = 17640;
                byte[] buffer = new byte[bufferSize];
                int bytesRead;

                while ((bytesRead = resampler.Read(buffer, 0, buffer.Length)) > 0 && !token.IsCancellationRequested)
                {
                    // NO COMPRESSION: We send the raw PCM bytes directly.
                    // If the last chunk is smaller than buffer, resize it so we don't send silence.
                    if (bytesRead < bufferSize)
                    {
                        byte[] finalChunk = new byte[bytesRead];
                        Array.Copy(buffer, finalChunk, bytesRead);
                        await _broadcaster.BroadcastAudioChunkAsync(finalChunk);
                    }
                    else
                    {
                        await _broadcaster.BroadcastAudioChunkAsync(buffer);
                    }

                    // TIMING:
                    // 17640 bytes / 176400 bytes/sec = 0.1 seconds (100ms)
                    // Wait slightly less to keep client buffer full (90ms)
                    await Task.Delay(90, token);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Radio] Error: {ex.Message}");
                await Task.Delay(1000, token);
            }
        }
    }
}