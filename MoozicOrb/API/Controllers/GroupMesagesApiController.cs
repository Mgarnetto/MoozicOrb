using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MoozicOrb.Api.Models;
using MoozicOrb.Api.Services.Interfaces;
using MoozicOrb.Hubs;
using MoozicOrb.Services;
using System.Text.Json;

[ApiController]
[Route("api/groups/{groupId:long}/messages")]
public class GroupMessagesController : ControllerBase
{
    private readonly IGroupMessageApiService _service;
    private readonly IHttpContextAccessor _http;
    private readonly IHubContext<MessageHub> _hub;

    public GroupMessagesController(
        IGroupMessageApiService service,
        IHttpContextAccessor http,
        IHubContext<MessageHub> hub)
    {
        _service = service;
        _http = http;
        _hub = hub;
    }

    private int GetUserId()
    {
        var sid = _http.HttpContext?.Request.Headers["X-Session-Id"].ToString();
        var session = SessionStore.GetSession(sid);
        if (session == null)
            throw new UnauthorizedAccessException();
        return session.UserId;
    }

    // ✅ Get list
    [HttpGet]
    public ActionResult<IEnumerable<GroupMessageDto>> GetMessages(
        long groupId, int? limit = null)
    {
        var msgs = _service.GetGroupMessages(groupId);

        if (limit.HasValue)
            msgs = msgs.Take(limit.Value).ToList();

        return Ok(msgs);
    }

    // ✅ Get single message
    [HttpGet("{messageId:long}")]
    public ActionResult<GroupMessageDto> GetMessage(
        long groupId,
        long messageId)
    {
        var msg = _service.GetGroupMessage(groupId, messageId);
        if (msg == null)
            return NotFound();

        //var sender = new MoozicOrb.IO.UserQuery().GetUserById(msg.SenderId);
        //msg.SenderName = sender.FirstName + " " + sender.LastName;

        return Ok(msg);
    }

    // ✅ Create
    [HttpPost]
    public async Task<IActionResult> CreateMessage(
        long groupId,
        [FromBody] JsonElement body)
    {
        if (!body.TryGetProperty("text", out var t))
            return BadRequest();

        int userId = GetUserId();
        string text = t.GetString();

        var messageId =
            _service.CreateGroupMessage(groupId, userId, text);

        // 🔔 notify group
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








