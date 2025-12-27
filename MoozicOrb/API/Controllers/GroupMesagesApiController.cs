using Microsoft.AspNetCore.Mvc;
using MoozicOrb.Api.Models;
using MoozicOrb.Api.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace MoozicOrb.Api.Controllers
{
    [ApiController]
    [Route("api/groups/{groupId}/messages")]
    public class GroupMessagesController : ControllerBase
    {
        private readonly IGroupMessageApiService _service;
        private readonly IWebHostEnvironment _env;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public GroupMessagesController(
            IGroupMessageApiService service,
            IWebHostEnvironment env,
            IHttpContextAccessor httpContextAccessor)
        {
            _service = service;
            _env = env;
            _httpContextAccessor = httpContextAccessor;
        }

        // GET all messages in group (optional limit)
        [HttpGet]
        public ActionResult<IEnumerable<GroupMessageDto>> GetMessages(long groupId, int? limit = null)
        {
            var messages = _service.GetGroupMessages(groupId);

            if (limit.HasValue)
                messages = new List<GroupMessageDto>(messages).GetRange(0, System.Math.Min(limit.Value, messages is ICollection<GroupMessageDto> col ? col.Count : 0));

            // Optionally hydrate profile pics / sender names here
            foreach (var msg in messages)
            {
                msg.SenderName ??= $"User{msg.SenderId}";
                msg.SenderProfilePicUrl ??= $"/images/users/{msg.SenderId}.png";
            }

            return Ok(messages);
        }

        // GET a single message by ID
        [HttpGet("{messageId}")]
        public ActionResult<GroupMessageDto> GetMessage(long groupId, long messageId)
        {
            var message = _service.GetGroupMessage(groupId, messageId);
            if (message == null) return NotFound();

            message.SenderName ??= $"User{message.SenderId}";
            message.SenderProfilePicUrl ??= $"/images/users/{message.SenderId}.png";

            return Ok(message);
        }

        // POST create a new message
        [HttpPost]
        public ActionResult CreateMessage(long groupId, [FromBody] dynamic body)
        {
            int senderId = 1; // TODO: get from auth context
            string text = body.GetProperty("text").GetString();

            var messageId = _service.CreateGroupMessage(groupId, senderId, text);

            return Ok(new { messageId });
        }
    }
}




