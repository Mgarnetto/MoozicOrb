using Microsoft.AspNetCore.Mvc;
using MoozicOrb.Api.Services.Interfaces;

namespace MoozicOrb.Api.Controllers
{
    [ApiController]
    [Route("api/groups/{groupId}/messages")]
    public class GroupMessagesController : ControllerBase
    {
        private readonly IGroupMessageApiService _service;

        public GroupMessagesController(IGroupMessageApiService service)
        {
            _service = service;
        }

        [HttpPost]
        public IActionResult CreateMessage(long groupId, [FromBody] dynamic body)
        {
            //int senderId = /* auth context */;
            int senderId = 1;
            string text = body.GetProperty("text").GetString();

            var messageId = _service.CreateGroupMessage(groupId, senderId, text);
            return Ok(new { messageId });
        }
    }
}

