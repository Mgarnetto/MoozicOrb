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
            // Path: ProjectRoot/MoozicOrb/media/[Type]
            string uploadPath = Path.Combine(_env.ContentRootPath, "MoozicOrb", "media", typeFolder);

            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            string ext = Path.GetExtension(file.FileName).ToLower();
            string uniqueName = $"{Guid.NewGuid()}{ext}";
            string fullPath = Path.Combine(uploadPath, uniqueName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return relative path for DB: "MoozicOrb/media/Audio/guid.mp3"
            return Path.Combine("MoozicOrb", "media", typeFolder, uniqueName).Replace("\\", "/");
        }

        public string GetPhysicalPath(string relativePath)
        {
            return Path.Combine(_env.ContentRootPath, relativePath);
        }
    }
}