using Microsoft.AspNetCore.Mvc;
using MoozicOrb.Api.Models;
using MoozicOrb.Api.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace MoozicOrb.Api.Controllers
{
    [ApiController]
    [Route("api/direct/messages")]
    public class DirectMessagesController : ControllerBase
    {
        private readonly IDirectMessageApiService _service;
        private readonly IWebHostEnvironment _env;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DirectMessagesController(
            IDirectMessageApiService service,
            IWebHostEnvironment env,
            IHttpContextAccessor httpContextAccessor)
        {
            _service = service;
            _env = env;
            _httpContextAccessor = httpContextAccessor;
        }

        // GET all messages between two users (optional limit)
        [HttpGet("{userId1}/{userId2}")]
        public ActionResult<IEnumerable<DirectMessageDto>> GetMessages(int userId1, int userId2, int? limit = null)
        {
            // need to check auth to ensure user is part of this DM

            var messages = _service.GetDirectMessages(userId1, userId2);

            if (limit.HasValue)
                messages = new List<DirectMessageDto>(messages).GetRange(0, System.Math.Min(limit.Value, messages is ICollection<DirectMessageDto> col ? col.Count : 0));

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
        public ActionResult<DirectMessageDto> GetMessage(long messageId)
        {
            // need to check auth to ensure user is part of this DM

            var message = _service.GetDirectMessage(messageId);
            if (message == null) return NotFound();

            message.SenderName ??= $"User{message.SenderId}";
            message.SenderProfilePicUrl ??= $"/images/users/{message.SenderId}.png";

            return Ok(message);
        }

        // POST create a new direct message
        [HttpPost]
        public ActionResult CreateMessage([FromBody] dynamic body)
        {
            int senderId = 1; // TODO: get from auth context
            int receiverId = body.GetProperty("receiverId").GetInt32();
            string text = body.GetProperty("text").GetString();

            var messageId = _service.CreateDirectMessage(senderId, receiverId, text);

            return Ok(new { messageId });
        }
    }
}

