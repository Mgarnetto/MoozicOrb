using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MoozicOrb.Api.Services;
using MoozicOrb.Services;
using System;
using System.Threading.Tasks;

namespace MoozicOrb.Api.Controllers
{
    [ApiController]
    [Route("api/upload")]
    public class UploadController : ControllerBase
    {
        private readonly IMediaUploadService _service;
        private readonly IHttpContextAccessor _http;

        public UploadController(
            IMediaUploadService service,
            IHttpContextAccessor http)
        {
            _service = service;
            _http = http;
        }

        private int GetUserId()
        {
            var sid = _http.HttpContext?.Request.Headers["X-Session-Id"].ToString();
            var session = SessionStore.GetSession(sid);
            if (session == null)
                throw new UnauthorizedAccessException();
            return session.UserId;
        }

        // POST: api/upload/track
        [HttpPost("track")]
        public async Task<IActionResult> UploadTrack(
            [FromForm] IFormFile file,
            [FromForm] string title,
            [FromForm] int visibility = 1)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            try
            {
                int userId = GetUserId();

                // Service handles Disk I/O + Duration Detection + Database
                long newTrackId = await _service.UploadTrackAsync(file, title, userId, visibility);

                return Ok(new { trackId = newTrackId });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Upload failed: {ex.Message}");
            }
        }
    }
}