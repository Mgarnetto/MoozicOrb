using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MoozicOrb.API.Services;
using MoozicOrb.Hubs;
using MoozicOrb.IO;
using MoozicOrb.Services; // For SessionStore
using System;
using System.Threading.Tasks;

namespace MoozicOrb.API.Controllers
{
    [ApiController]
    [Route("api/upload")]
    public class UploadController : ControllerBase
    {
        private readonly IMediaFileService _fileService;
        private readonly IMediaProcessor _processor;
        private readonly IHubContext<PostHub> _hub; // Using PostHub for content updates
        private readonly IHttpContextAccessor _http;

        public UploadController(IMediaFileService f, IMediaProcessor p, IHubContext<PostHub> h, IHttpContextAccessor http)
        {
            _fileService = f; _processor = p; _hub = h; _http = http;
        }

        private int GetUserId()
        {
            var sid = _http.HttpContext?.Request.Headers["X-Session-Id"].ToString();
            var session = SessionStore.GetSession(sid);
            if (session == null) throw new UnauthorizedAccessException();
            return session.UserId;
        }

        [HttpPost("audio")]
        public async Task<IActionResult> UploadAudio([FromForm] IFormFile file, [FromForm] string title)
        {
            try
            {
                int uid = GetUserId();
                string relPath = await _fileService.SaveFileAsync(file, "Audio");
                var meta = await _processor.ProcessAudioAsync(_fileService.GetPhysicalPath(relPath), relPath);

                long newId = new InsertAudio().Execute(uid, title, meta.RelativePath, meta.SnippetPath, meta.DurationSeconds);

                // NOTIFY CLIENT: They receive ID, then query /api/media/audio/{id} to display it
                await _hub.Clients.User(uid.ToString()).SendAsync("MediaReady", new { id = newId, type = "audio" });
                return Ok(new { id = newId });
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpPost("video")]
        public async Task<IActionResult> UploadVideo([FromForm] IFormFile file, [FromForm] string title)
        {
            try
            {
                int uid = GetUserId();
                string relPath = await _fileService.SaveFileAsync(file, "Video");
                var meta = await _processor.ProcessVideoAsync(_fileService.GetPhysicalPath(relPath), relPath);

                long newId = new InsertVideo().Execute(uid, title, meta.RelativePath, meta.SnippetPath, meta.DurationSeconds, meta.Width, meta.Height);

                await _hub.Clients.User(uid.ToString()).SendAsync("MediaReady", new { id = newId, type = "video" });
                return Ok(new { id = newId });
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpPost("image")]
        public async Task<IActionResult> UploadImage([FromForm] IFormFile file, [FromForm] string title)
        {
            try
            {
                int uid = GetUserId();
                string relPath = await _fileService.SaveFileAsync(file, "Image");
                var meta = await _processor.ProcessImageAsync(_fileService.GetPhysicalPath(relPath), relPath);

                long newId = new InsertImage().Execute(uid, title, meta.RelativePath, meta.Width, meta.Height);

                await _hub.Clients.User(uid.ToString()).SendAsync("MediaReady", new { id = newId, type = "image" });
                return Ok(new { id = newId });
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }
    }
}