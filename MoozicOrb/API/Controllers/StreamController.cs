//using Microsoft.AspNetCore.Mvc;
//using MoozicOrb.Api.Models;
//using MoozicOrb.Api.Services.Interfaces;
//using MoozicOrb.Services; // For SessionStore

//namespace MoozicOrb.Api.Controllers
//{
//    [ApiController]
//    [Route("api/stream")]
//    public class StreamController : ControllerBase
//    {
//        private readonly IStreamApiService _service;
//        private readonly IHttpContextAccessor _http;

//        public StreamController(
//            IStreamApiService service,
//            IHttpContextAccessor http)
//        {
//            _service = service;
//            _http = http;
//        }

//        private int GetUserId()
//        {
//            var sid = _http.HttpContext?.Request.Headers["X-Session-Id"].ToString();
//            // Fallback: If streaming via standard HTML5 audio tag, headers might be tricky.
//            // We often pass session via query param for audio tags: ?sid=...
//            if (string.IsNullOrEmpty(sid))
//            {
//                sid = _http.HttpContext?.Request.Query["sid"].ToString();
//            }

//            var session = SessionStore.GetSession(sid);
//            if (session == null)
//                throw new UnauthorizedAccessException();

//            return session.UserId;
//        }

//        // ✅ Stream Track
//        [HttpGet("{trackId:long}")]
//        public IActionResult GetStream(long trackId)
//        {
//            // 1. Get Metadata
//            var info = _service.GetStreamInfo(trackId);
//            if (info == null) return NotFound();

//            // 2. Check Logic (Ban / Visibility)
//            if (info.IsUploaderBanned)
//                return StatusCode(403, "Content unavailable.");

//            // 0 = Private, 1 = Public, 2 = Unlisted
//            if (info.Visibility == 0)
//            {
//                try
//                {
//                    int userId = GetUserId();
//                    if (userId != info.UploadedByUserId)
//                        return Unauthorized();
//                }
//                catch (UnauthorizedAccessException)
//                {
//                    return Unauthorized();
//                }
//            }

//            // 3. Hand off to Windows IIS (Kernel Mode)
//            return PhysicalFile(info.PhysicalPath, info.MimeType, enableRangeProcessing: true);
//        }
//    }
//}
