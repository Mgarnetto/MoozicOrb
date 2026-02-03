using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MoozicOrb.API.Services
{
    public interface IMediaFileService
    {
        Task<string> SaveFileAsync(IFormFile file, string typeFolder);
        string GetPhysicalPath(string relativePath);
    }

    public class MediaFileService : IMediaFileService
    {
        private readonly IWebHostEnvironment _env;

        public MediaFileService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<string> SaveFileAsync(IFormFile file, string typeFolder)
        {
            // FIX: Use WebRootPath to save into 'wwwroot'
            string uploadPath = Path.Combine(_env.WebRootPath, "media", typeFolder);

            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            string ext = Path.GetExtension(file.FileName).ToLower();
            // Using GUID to prevent name collisions
            string uniqueName = $"{Guid.NewGuid()}{ext}";
            string fullPath = Path.Combine(uploadPath, uniqueName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return clean URL path: "media/Image/file.jpg"
            return Path.Combine("media", typeFolder, uniqueName).Replace("\\", "/");
        }

        public string GetPhysicalPath(string relativePath)
        {
            // FIX: Map the relative path back to wwwroot for processing
            return Path.Combine(_env.WebRootPath, relativePath);
        }
    }
}