using MoozicOrb.API.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xabe.FFmpeg;

namespace MoozicOrb.API.Services
{
    public interface IMediaProcessor
    {
        Task<MediaMetadata> ProcessAudioAsync(string physicalPath, string relativePath);
        Task<MediaMetadata> ProcessVideoAsync(string physicalPath, string relativePath);
        Task<MediaMetadata> ProcessImageAsync(string physicalPath, string relativePath);
    }

    public class MediaProcessor : IMediaProcessor
    {
        public async Task<MediaMetadata> ProcessAudioAsync(string physicalPath, string relativePath)
        {
            var meta = new MediaMetadata { RelativePath = relativePath };

            // 1. Get Duration
            try
            {
                using (var t = TagLib.File.Create(physicalPath))
                {
                    meta.DurationSeconds = (int)t.Properties.Duration.TotalSeconds;
                }
            }
            catch { meta.DurationSeconds = 0; }

            // 2. Generate 30s Snippet
            string snippetName = Path.GetFileNameWithoutExtension(physicalPath) + "_snippet.mp3";
            string snippetPhys = Path.Combine(Path.GetDirectoryName(physicalPath), snippetName);

            try
            {
                // Cut 00:00 to 00:30
                var conversion = await FFmpeg.Conversions.FromSnippet.Split(physicalPath, snippetPhys, TimeSpan.Zero, TimeSpan.FromSeconds(30));
                await conversion.Start();
                meta.SnippetPath = relativePath.Replace(Path.GetFileName(physicalPath), snippetName).Replace("\\", "/");
            }
            catch { /* Snippet failed, proceed without it */ }

            return meta;
        }

        public async Task<MediaMetadata> ProcessVideoAsync(string physicalPath, string relativePath)
        {
            var meta = new MediaMetadata { RelativePath = relativePath };

            try
            {
                IMediaInfo info = await FFmpeg.GetMediaInfo(physicalPath);
                var vStream = info.VideoStreams.FirstOrDefault();

                if (vStream != null)
                {
                    meta.DurationSeconds = (int)info.Duration.TotalSeconds;
                    meta.Width = vStream.Width;
                    meta.Height = vStream.Height;

                    // Generate Thumbnail at frame 10
                    string thumbName = Path.GetFileNameWithoutExtension(physicalPath) + "_thumb.jpg";
                    string thumbPhys = Path.Combine(Path.GetDirectoryName(physicalPath), thumbName);

                    await FFmpeg.Conversions.New()
                        .AddStream(vStream)
                        .ExtractNthFrame(10, (s) => thumbPhys)
                        .Start();

                    meta.SnippetPath = relativePath.Replace(Path.GetFileName(physicalPath), thumbName).Replace("\\", "/");
                }
            }
            catch { /* Processing failed */ }

            return meta;
        }

        public async Task<MediaMetadata> ProcessImageAsync(string physicalPath, string relativePath)
        {
            var meta = new MediaMetadata { RelativePath = relativePath };
            try
            {
                using (var image = TagLib.File.Create(physicalPath) as TagLib.Image.File)
                {
                    if (image != null)
                    {
                        meta.Width = image.Properties.PhotoWidth;
                        meta.Height = image.Properties.PhotoHeight;
                    }
                }
            }
            catch { }
            return meta;
        }
    }
}