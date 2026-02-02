using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MoozicOrb.API.Services; // Ensure this contains IMediaFileService/Processor
using MoozicOrb.Hubs;
using MoozicOrb.IO;
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
        private readonly IHubContext<PostHub> _hub;
        private readonly IHttpContextAccessor _http;

        public UploadController(IMediaFileService f, IMediaProcessor p, IHubContext<PostHub> h, IHttpContextAccessor http)
        {
            _fileService = f; _processor = p; _hub = h; _http = http;
        }

        private int GetUserId()
        {
            // Fallback for dev/testing if session is missing
            if (_http.HttpContext?.Request.Headers.ContainsKey("X-Session-Id") == false) return 105;

            var sid = _http.HttpContext?.Request.Headers["X-Session-Id"].ToString();
            var session = SessionStore.GetSession(sid);
            if (session == null) throw new UnauthorizedAccessException();
            return session.UserId;
        }

        // ==========================================
        // SMART DISPATCHER (For generic Feed uploads)
        // ==========================================
        [HttpPost("")]
        public async Task<IActionResult> UploadUniversal([FromForm] IFormFile file)
        {
            if (file == null) return BadRequest("No file provided");

            // Auto-detect type based on extension
            string ext = Path.GetExtension(file.FileName).ToLower();

            if (ext == ".mp3" || ext == ".wav" || ext == ".ogg" || ext == ".m4a")
                return await UploadAudio(file, file.FileName);

            if (ext == ".mp4" || ext == ".mov" || ext == ".webm")
                return await UploadVideo(file, file.FileName);

            // Default to Image for jpg, png, gif, etc.
            return await UploadImage(file, file.FileName);
        }

        // ==========================================
        // SPECIFIC HANDLERS
        // ==========================================

        [HttpPost("audio")]
        public async Task<IActionResult> UploadAudio([FromForm] IFormFile file, [FromForm] string title = null)
        {
            try
            {
                int uid = GetUserId();
                string finalTitle = title ?? file.FileName;

                // 1. Save Physical File
                string relPath = await _fileService.SaveFileAsync(file, "Audio");

                // 2. Extract Metadata (Duration, Waveform)
                var meta = await _processor.ProcessAudioAsync(_fileService.GetPhysicalPath(relPath), relPath);

                // 3. Database Insert
                long newId = new InsertAudio().Execute(uid, finalTitle, meta.RelativePath, meta.SnippetPath, meta.DurationSeconds);

                // 4. Return Data (ID + Type + URL for preview)
                return Ok(new { id = newId, type = 1, url = relPath });
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpPost("video")]
        public async Task<IActionResult> UploadVideo([FromForm] IFormFile file, [FromForm] string title = null)
        {
            try
            {
                int uid = GetUserId();
                string finalTitle = title ?? file.FileName;

                string relPath = await _fileService.SaveFileAsync(file, "Video");
                var meta = await _processor.ProcessVideoAsync(_fileService.GetPhysicalPath(relPath), relPath);

                long newId = new InsertVideo().Execute(uid, finalTitle, meta.RelativePath, meta.SnippetPath, meta.DurationSeconds, meta.Width, meta.Height);

                return Ok(new { id = newId, type = 2, url = relPath });
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpPost("image")]
        public async Task<IActionResult> UploadImage([FromForm] IFormFile file, [FromForm] string title = null)
        {
            try
            {
                int uid = GetUserId();
                string finalTitle = title ?? file.FileName;

                string relPath = await _fileService.SaveFileAsync(file, "Image");

                // Note: Ensure your ProcessImageAsync handles logic if width/height are null
                var meta = await _processor.ProcessImageAsync(_fileService.GetPhysicalPath(relPath), relPath);

                long newId = new InsertImage().Execute(uid, finalTitle, meta.RelativePath, meta.Width, meta.Height);

                return Ok(new { id = newId, type = 3, url = relPath });
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }
    }
}