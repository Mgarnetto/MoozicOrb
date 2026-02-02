using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MoozicOrb.API.Services;
using MoozicOrb.Hubs;
using MoozicOrb.IO;
using MoozicOrb.Services;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MoozicOrb.API.Controllers
{
    [ApiController]
    [Route("api/upload")]
    public class UploadController : ControllerBase
    {
        private readonly IMediaFileService _fileService;
        private readonly IMediaProcessor _processor;
        private readonly IHubContext<PostHub> _hub;
        private readonly IHttpContextAccessor _http;

        public UploadController(IMediaFileService f, IMediaProcessor p, IHubContext<PostHub> h, IHttpContextAccessor http)
        {
            _fileService = f; _processor = p; _hub = h; _http = http;
        }

        private int GetUserId()
        {
            // ... your session logic ...
            return 105; // Placeholder
        }

        // SMART DISPATCHER
        [HttpPost("")]
        public async Task<IActionResult> UploadUniversal([FromForm] IFormFile file)
        {
            if (file == null) return BadRequest("No file");
            string ext = Path.GetExtension(file.FileName).ToLower();

            if (ext == ".mp3" || ext == ".wav" || ext == ".ogg") return await UploadAudio(file);
            if (ext == ".mp4" || ext == ".mov") return await UploadVideo(file);
            return await UploadImage(file);
        }

        private async Task<IActionResult> UploadImage(IFormFile file)
        {
            try
            {
                int uid = GetUserId();

                // 1. Save (Returns "MoozicOrb/media/Image/guid.jpg")
                string dbPath = await _fileService.SaveFileAsync(file, "Image");

                // 2. Process Metadata (Use Path.Combine logic in service if needed)
                string physPath = Path.Combine(Directory.GetCurrentDirectory(), dbPath);
                var meta = await _processor.ProcessImageAsync(physPath, dbPath);

                // 3. Insert
                long newId = new InsertImage().Execute(uid, file.FileName, dbPath, meta.Width, meta.Height);

                // 4. Return URL for Frontend
                // Transform "MoozicOrb/media/Image/x.jpg" -> "/media/Image/x.jpg"
                string webUrl = "/" + dbPath.Replace("MoozicOrb/", "").Replace("\\", "/");

                return Ok(new { id = newId, type = 3, url = webUrl });
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        // (Implement UploadAudio and UploadVideo similarly, transforming the URL at the end)
        private async Task<IActionResult> UploadAudio(IFormFile file)
        {
            try
            {
                int uid = GetUserId();
                string dbPath = await _fileService.SaveFileAsync(file, "Audio");
                string physPath = Path.Combine(Directory.GetCurrentDirectory(), dbPath);
                var meta = await _processor.ProcessAudioAsync(physPath, dbPath);

                long newId = new InsertAudio().Execute(uid, file.FileName, dbPath, meta.SnippetPath, meta.DurationSeconds);

                string webUrl = "/" + dbPath.Replace("MoozicOrb/", "").Replace("\\", "/");
                return Ok(new { id = newId, type = 1, url = webUrl });
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        private async Task<IActionResult> UploadVideo(IFormFile file)
        {
            try
            {
                int uid = GetUserId();
                string dbPath = await _fileService.SaveFileAsync(file, "Video");
                string physPath = Path.Combine(Directory.GetCurrentDirectory(), dbPath);
                var meta = await _processor.ProcessVideoAsync(physPath, dbPath);

                long newId = new InsertVideo().Execute(uid, file.FileName, dbPath, meta.SnippetPath, meta.DurationSeconds, meta.Width, meta.Height);

                string webUrl = "/" + dbPath.Replace("MoozicOrb/", "").Replace("\\", "/");
                return Ok(new { id = newId, type = 2, url = webUrl });
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }
    }
}