using Microsoft.AspNetCore.Mvc;
using MoozicOrb.IO;

namespace MoozicOrb.API.Controllers
{
    [ApiController]
    [Route("api/media")]
    public class MediaController : ControllerBase
    {
        [HttpGet("audio/{id}")]
        public IActionResult GetAudio(long id)
        {
            var data = new GetAudio().Execute(id);
            if (data == null) return NotFound();
            return Ok(new { url = data.RelativePath, duration = data.DurationSeconds });
        }

        [HttpGet("video/{id}")]
        public IActionResult GetVideo(long id)
        {
            var data = new GetVideo().Execute(id);
            if (data == null) return NotFound();
            return Ok(new { url = data.RelativePath, thumb = data.SnippetPath, w = data.Width, h = data.Height, duration = data.DurationSeconds });
        }

        [HttpGet("image/{id}")]
        public IActionResult GetImage(long id)
        {
            var data = new GetImage().Execute(id);
            if (data == null) return NotFound();
            return Ok(new { url = data.RelativePath, w = data.Width, h = data.Height });
        }
    }
}