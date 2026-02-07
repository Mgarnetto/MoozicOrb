using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MoozicOrb.Api.Models;
using MoozicOrb.Api.Services.Interfaces;
using MoozicOrb.Hubs;
using MoozicOrb.Services;
using MoozicOrb.IO; 
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

[ApiController]
[Route("api/direct/messages")]
public class DirectMessagesController : ControllerBase
{
    private readonly IDirectMessageApiService _service;
    private readonly IHttpContextAccessor _http;
    private readonly IHubContext<MessageHub> _hub;
    private readonly UserConnectionManager _connections;
    private readonly UserQuery _userQuery;
    private readonly NotificationService _notify; // <--- ADDED

    public DirectMessagesController(
        IDirectMessageApiService service,
        IHttpContextAccessor http,
        IHubContext<MessageHub> hub,
        UserConnectionManager connections,
        NotificationService notify) // <--- INJECTED
    {
        _service = service;
        _http = http;
        _hub = hub;
        _connections = connections;
        _userQuery = new UserQuery();
        _notify = notify;
    }

    // =========================================
    // Helpers
    // =========================================
    private int GetUserId()
    {
        var sid = _http.HttpContext?.Request.Headers["X-Session-Id"].ToString();
        var session = SessionStore.GetSession(sid);

        if (session == null)
            throw new UnauthorizedAccessException();

        return session.UserId;
    }

    // =========================================
    // GET: User Info for Chat Header
    // =========================================
    [HttpGet("user-info/{targetUserId:int}")]
    public IActionResult GetUserInfo(int targetUserId)
    {
        GetUserId(); // Auth check

        var user = _userQuery.GetUserById(targetUserId);
        if (user == null) return NotFound();

        return Ok(new
        {
            id = user.UserId,
            name = !string.IsNullOrEmpty(user.DisplayName) ? user.DisplayName : user.UserName,
            img = !string.IsNullOrEmpty(user.ProfilePic) ? user.ProfilePic : "/img/profile_default.jpg"
        });
    }

    // =========================================
    // GET: Conversation
    // =========================================
    [HttpGet("with/{otherUserId:int}")]
    public ActionResult<IEnumerable<DirectMessageDto>> GetConversation(int otherUserId)
    {
        int me = GetUserId();
        var messages = _service.GetDirectMessages(me, otherUserId);
        return Ok(messages);
    }

    // =========================================
    // GET: Single message
    // =========================================
    [HttpGet("single/{messageId:long}")]
    public ActionResult<DirectMessageDto> GetMessage(long messageId)
    {
        int me = GetUserId();
        var msg = _service.GetDirectMessage(messageId);
        if (msg == null) return NotFound();
        if (msg.SenderId != me && msg.ReceiverId != me) return Forbid();
        return Ok(msg);
    }

    // =========================================
    // POST: Send DM
    // =========================================
    [HttpPost]
    public async Task<IActionResult> CreateMessage([FromBody] JsonElement body)
    {
        if (!body.TryGetProperty("receiverId", out var r) ||
            !body.TryGetProperty("text", out var t))
            return BadRequest();

        int senderId = GetUserId();
        int receiverId = r.GetInt32();
        string text = t.GetString();

        var messageId = _service.CreateDirectMessage(senderId, receiverId, text);

        // 1. 🔔 Notify recipient via SignalR (Live Chat)
        foreach (var conn in _connections.GetConnections(receiverId))
        {
            await _hub.Clients.Client(conn).SendAsync("OnDirectMessage", new { senderId, messageId });
        }

        // 2. 🔔 Notify sender (Live Chat - Multi-tab)
        foreach (var conn in _connections.GetConnections(senderId))
        {
            await _hub.Clients.Client(conn).SendAsync("OnDirectMessage", new { senderId, messageId });
        }

        // 3. 🔔 SYSTEM NOTIFICATION (New Badge/Toast for recipient)
        // This ensures they see it even if they aren't looking at the chat window
        await _notify.NotifyUser(receiverId, senderId, "message", senderId, "sent you a message");

        return Ok(new { messageId });
    }

    // =========================================
    // GET: Bootstrap
    // =========================================
    [HttpGet]
    public ActionResult GetAllDirectMessages()
    {
        int me = GetUserId();
        var conversations = _service.GetAllDirectMessages(me);
        return Ok(new
        {
            users = conversations.Keys,
            messages = conversations
        });
    }
}




