using Microsoft.AspNetCore.Mvc;
using MoozicOrb.Api.Services.Interfaces;

namespace MoozicOrb.Api.Controllers
{
    [ApiController]
    [Route("api/dm")]
    public class DirectMessagesController : ControllerBase
    {
        private readonly IDirectMessageApiService _service;

        public DirectMessagesController(IDirectMessageApiService service)
        {
            _service = service;
        }

        [HttpPost("{receiverId}")]
        public IActionResult SendMessage(int receiverId, [FromBody] dynamic body)
        {
            //int senderId = /* auth context */;
            int senderId = 1;

            string text = body.GetProperty("text").GetString();

            var messageId = _service.CreateDirectMessage(senderId, receiverId, text);
            return Ok(new { messageId });
        }
    }
}

