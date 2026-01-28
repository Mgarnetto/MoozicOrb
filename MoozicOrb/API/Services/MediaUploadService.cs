using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using MoozicOrb.IO;
using System;
using System.IO;
using System.Threading.Tasks;
// Note: We do NOT add 'using TagLib;' at the top to avoid conflicts with System.IO.File

namespace MoozicOrb.Api.Services
{
    public class MediaUploadService : IMediaUploadService
    {
        private readonly string _storagePath;

        public MediaUploadService(IConfiguration config)
        {
            _storagePath = config["Storage:SecurePath"] ?? @"D:\MoozicOrb_Assets\Protected\";
        }

        // Removed 'duration' from parameters. The server calculates it now.
        public async Task<long> UploadTrackAsync(IFormFile file, string title, int uploaderId, int visibility)
        {
            // 1. EXTENSION CHECK
            var ext = Path.GetExtension(file.FileName).ToLower();
            if (ext != ".mp3") throw new Exception("Only MP3 files are allowed.");

            // 2. STORAGE PATH
            var uniqueFileName = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(_storagePath, uniqueFileName);
            Directory.CreateDirectory(_storagePath);

            // 3. WRITE TO DISK
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // 4. METADATA EXTRACTION (TagLibSharp)
            int durationSeconds = 0;
            try
            {
                // We use the fully qualified name to avoid ambiguity
                var tfile = TagLib.File.Create(fullPath);
                durationSeconds = (int)tfile.Properties.Duration.TotalSeconds;

                // Optional: Read Artist/Title from metadata if not provided?
                // if (string.IsNullOrEmpty(title)) title = tfile.Tag.Title;
            }
            catch (Exception ex)
            {
                // Log warning: "Could not read duration"
                Console.WriteLine($"Metadata Error: {ex.Message}");
                // We proceed with duration=0 rather than failing the whole upload
            }

            // 5. DATABASE INSERT
            var io = new InsertTrack();
            return io.Execute(title, uploaderId, fullPath, durationSeconds, uploaderId, visibility);
        }
    }

    public interface IMediaUploadService
    {
        Task<long> UploadTrackAsync(IFormFile file, string title, int uploaderId, int visibility);
    }
}