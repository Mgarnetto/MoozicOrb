using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MoozicOrb.API.Services;
using MoozicOrb.IO; // Contains InsertAudio, InsertVideo, InsertImage
using MoozicOrb.Services; // For SessionStore
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
            // 1. Try Header (SPA/API way)
            var sid = _http.HttpContext?.Request.Headers["X-Session-Id"].ToString();
            if (!string.IsNullOrEmpty(sid))
            {
                var session = SessionStore.GetSession(sid);
                if (session != null) return session.UserId;
            }

            // 2. Try Cookie (Fallback)
            int? cookieId = _http.HttpContext?.Session.GetInt32("UserId");
            if (cookieId.HasValue && cookieId.Value > 0) return cookieId.Value;

            return 0;
        }

        // SMART DISPATCHER
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

            // Default to Image
            return await UploadImage(file, uid);
        }

        private async Task<IActionResult> UploadImage(IFormFile file, int uid)
        {
            try
            {
                // 1. Save
                string dbPath = await _fileService.SaveFileAsync(file, "Image");

                // 2. Process (Get Width/Height)
                string physPath = _fileService.GetPhysicalPath(dbPath);
                var meta = await _processor.ProcessImageAsync(physPath, dbPath);

                // 3. Insert using specific class
                long newId = new InsertImage().Execute(uid, file.FileName, dbPath, meta.Width, meta.Height);

                // 4. Return
                string webUrl = "/" + dbPath.Replace("MoozicOrb/", "").Replace("\\", "/");
                return Ok(new { id = newId, type = 3, url = webUrl });
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        private async Task<IActionResult> UploadAudio(IFormFile file, int uid)
        {
            try
            {
                string dbPath = await _fileService.SaveFileAsync(file, "Audio");
                string physPath = _fileService.GetPhysicalPath(dbPath);
                var meta = await _processor.ProcessAudioAsync(physPath, dbPath);

                // Using InsertAudio class
                long newId = new InsertAudio().Execute(uid, file.FileName, dbPath, meta.SnippetPath, meta.DurationSeconds);

                string webUrl = "/" + dbPath.Replace("MoozicOrb/", "").Replace("\\", "/");
                return Ok(new { id = newId, type = 1, url = webUrl });
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        private async Task<IActionResult> UploadVideo(IFormFile file, int uid)
        {
            try
            {
                string dbPath = await _fileService.SaveFileAsync(file, "Video");
                string physPath = _fileService.GetPhysicalPath(dbPath);
                var meta = await _processor.ProcessVideoAsync(physPath, dbPath);

                // Using InsertVideo class
                long newId = new InsertVideo().Execute(uid, file.FileName, dbPath, meta.SnippetPath, meta.DurationSeconds, meta.Width, meta.Height);

                string webUrl = "/" + dbPath.Replace("MoozicOrb/", "").Replace("\\", "/");
                return Ok(new { id = newId, type = 2, url = webUrl });
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }
    }
}