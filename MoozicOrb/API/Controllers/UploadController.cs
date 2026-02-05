using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MoozicOrb.API.Services;
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
        private readonly IHttpContextAccessor _http;

        public UploadController(IMediaFileService f, IMediaProcessor p, IHttpContextAccessor http)
        {
            _fileService = f;
            _processor = p;
            _http = http;
        }

        private int GetUserId()
        {
            var sid = _http.HttpContext?.Request.Headers["X-Session-Id"].ToString();
            if (!string.IsNullOrEmpty(sid))
            {
                var session = SessionStore.GetSession(sid);
                if (session != null) return session.UserId;
            }
            // Fallback for cookies if used
            int? cookieId = _http.HttpContext?.Session.GetInt32("UserId");
            if (cookieId.HasValue && cookieId.Value > 0) return cookieId.Value;

            return 0;
        }

        [HttpPost("")]
        public async Task<IActionResult> UploadUniversal([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("No file provided");

            int uid = GetUserId();
            if (uid == 0) return Unauthorized("User not logged in");

            string ext = Path.GetExtension(file.FileName).ToLower();

            if (ext == ".mp3" || ext == ".wav" || ext == ".ogg" || ext == ".m4a")
                return await UploadAudio(file, uid);

            if (ext == ".mp4" || ext == ".mov" || ext == ".avi" || ext == ".webm")
                return await UploadVideo(file, uid);

            return await UploadImage(file, uid);
        }

        private async Task<IActionResult> UploadImage(IFormFile file, int uid)
        {
            try
            {
                string relativePath = await _fileService.SaveFileAsync(file, "Image");
                string physPath = _fileService.GetPhysicalPath(relativePath);
                int width = 0, height = 0;

                // TOLERANT PROCESSING
                try
                {
                    var meta = await _processor.ProcessImageAsync(physPath, relativePath);
                    width = meta.Width;
                    height = meta.Height;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Upload] Image metadata failed: {ex.Message}. Continuing...");
                }

                string webUrl = "/" + relativePath.Replace("\\", "/");

                // Use your new InsertImage class
                long newId = new InsertImage().Execute(uid, file.FileName, webUrl, width, height);

                return Ok(new { id = newId, type = 3, url = webUrl });
            }
            catch (Exception ex) { return BadRequest($"Image Upload Error: {ex.Message}"); }
        }

        private async Task<IActionResult> UploadAudio(IFormFile file, int uid)
        {
            try
            {
                // 1. Save File (Critical Step)
                string dbPath = await _fileService.SaveFileAsync(file, "Audio");
                string physPath = _fileService.GetPhysicalPath(dbPath);

                // 2. Prepare Defaults
                string snippetPath = "";
                int duration = 0;

                // 3. Try Metadata (Optional Step)
                try
                {
                    var meta = await _processor.ProcessAudioAsync(physPath, dbPath);
                    snippetPath = meta.SnippetPath;
                    duration = (int)meta.DurationSeconds;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Upload] Audio processing failed: {ex.Message}. Continuing...");
                }

                // 4. Format Path
                string webUrl = "/" + dbPath.Replace("MoozicOrb/", "").Replace("\\", "/");

                // 5. Insert Record (Safe execution with new class)
                long newId = new InsertAudio().Execute(uid, file.FileName, webUrl, snippetPath, duration);

                return Ok(new { id = newId, type = 1, url = webUrl });
            }
            catch (Exception ex) { return BadRequest($"Audio Upload Error: {ex.Message}"); }
        }

        private async Task<IActionResult> UploadVideo(IFormFile file, int uid)
        {
            try
            {
                // 1. Save File
                string dbPath = await _fileService.SaveFileAsync(file, "Video");
                string physPath = _fileService.GetPhysicalPath(dbPath);

                // 2. Prepare Defaults
                string thumbPath = "";
                int duration = 0;
                int width = 0;
                int height = 0;

                // 3. Try Metadata
                try
                {
                    var meta = await _processor.ProcessVideoAsync(physPath, dbPath);
                    thumbPath = meta.SnippetPath;
                    duration = (int)meta.DurationSeconds;
                    width = meta.Width;
                    height = meta.Height;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Upload] Video processing failed: {ex.Message}. Continuing...");
                }

                // 4. Format Path
                string webUrl = "/" + dbPath.Replace("MoozicOrb/", "").Replace("\\", "/");

                // 5. Insert Record
                long newId = new InsertVideo().Execute(uid, file.FileName, webUrl, thumbPath, duration, width, height);

                return Ok(new { id = newId, type = 2, url = webUrl });
            }
            catch (Exception ex) { return BadRequest($"Video Upload Error: {ex.Message}"); }
        }
    }
}