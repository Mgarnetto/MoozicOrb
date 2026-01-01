using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MoozicOrb.Api.Models;
using MoozicOrb.Api.Services.Interfaces;
using MoozicOrb.Hubs;
using System.Collections.Generic;
using System.Text.Json;

namespace MoozicOrb.Api.Controllers
{
    [ApiController]
    [Route("api/groups/{groupId:long}/messages")]
    public class GroupMessagesController : ControllerBase
    {
        private readonly IGroupMessageApiService _service;
        private readonly IWebHostEnvironment _env;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHubContext<MessageHub> _hub;

        public GroupMessagesController(
            IGroupMessageApiService service,
            IWebHostEnvironment env,
            IHttpContextAccessor httpContextAccessor,
            IHubContext<MessageHub> hub)
        {
            _service = service;
            _env = env;
            _httpContextAccessor = httpContextAccessor;
            _hub = hub;
        }

        // ----------------------------------------------------
        // GET ALL messages in group (on connect / refresh)
        // ----------------------------------------------------
        [HttpGet]
        public ActionResult<IEnumerable<GroupMessageDto>> GetMessages(
            [FromRoute] long groupId,
            [FromQuery] int? limit = null)
        {
            var messages = _service.GetGroupMessages(groupId);

            if (limit.HasValue)
            {
                messages = new List<GroupMessageDto>(messages)
                    .GetRange(
                        0,
                        System.Math.Min(
                            limit.Value,
                            ((ICollection<GroupMessageDto>)messages).Count
                        )
                    );
            }

            var request = _httpContextAccessor.HttpContext?.Request;
            string baseUrl = $"{request.Scheme}://{request.Host}";

            foreach (var msg in messages)
            {
                msg.SenderName ??= $"User{msg.SenderId}";
                msg.SenderProfilePicUrl ??=
                    $"{baseUrl}/api/messages/media/image/{msg.SenderId}";
            }

            return Ok(messages);
        }

        // ----------------------------------------------------
        // GET SINGLE message (after SignalR notify)
        // ----------------------------------------------------
        [HttpGet("{messageId:long}")]
        public ActionResult<GroupMessageDto> GetMessage(
            [FromRoute] long groupId,
            [FromRoute] long messageId)
        {
            var message = _service.GetGroupMessage(groupId, messageId);
            if (message == null)
                return NotFound();

            var request = _httpContextAccessor.HttpContext?.Request;
            string baseUrl = $"{request.Scheme}://{request.Host}";

            message.SenderName ??= $"User{message.SenderId}";
            message.SenderProfilePicUrl ??=
                $"{baseUrl}/api/messages/media/image/{message.SenderId}";

            return Ok(message);
        }

        // ----------------------------------------------------
        // CREATE message (POST)
        // ----------------------------------------------------
        [HttpPost]
        public async Task<ActionResult> CreateMessage(
            [FromRoute] long groupId,
            [FromBody] JsonElement body)
        {
            if (!body.TryGetProperty("text", out var textProp))
                return BadRequest("Missing text");

            var text = textProp.GetString();
            if (string.IsNullOrWhiteSpace(text))
                return BadRequest("Empty message");

            int senderId = 1; // TODO: auth context

            var messageId =
                _service.CreateGroupMessage(groupId, senderId, text);

            // 🔔 Notify clients to pull
            await _hub.Clients
                .Group($"group-{groupId}")
                .SendAsync("OnGroupMessage", new
                {
                    groupId,
                    messageId
                });

            return Ok(new { messageId });
        }
    }
}







