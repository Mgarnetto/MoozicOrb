using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MoozicOrb.Api.Models;
using MoozicOrb.Api.Services.Interfaces;
using MoozicOrb.Hubs;
using MoozicOrb.Services;
using System.Text.Json;

[ApiController]
[Route("api/direct/messages")]
public class DirectMessagesController : ControllerBase
{
    private readonly IDirectMessageApiService _service;
    private readonly IHttpContextAccessor _http;
    private readonly IHubContext<MessageHub> _hub;
    private readonly UserConnectionManager _connections;

    public DirectMessagesController(
        IDirectMessageApiService service,
        IHttpContextAccessor http,
        IHubContext<MessageHub> hub,
        UserConnectionManager connections)
    {
        _service = service;
        _http = http;
        _hub = hub;
        _connections = connections;
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
    // GET: Conversation with user
    // Used by: loadDirectMessages(userId)
    // =========================================
    [HttpGet("with/{otherUserId:int}")]
    public ActionResult<IEnumerable<DirectMessageDto>> GetConversation(
        int otherUserId)
    {
        int me = GetUserId();

        var messages = _service.GetDirectMessages(me, otherUserId);
        return Ok(messages);
    }

    // =========================================
    // GET: Single message (SignalR fetch)
    // =========================================
    [HttpGet("single/{messageId:long}")]
    public ActionResult<DirectMessageDto> GetMessage(long messageId)
    {
        int me = GetUserId();

        var msg = _service.GetDirectMessage(messageId);
        if (msg == null)
            return NotFound();

        if (msg.SenderId != me && msg.ReceiverId != me)
            return Forbid();

        //var sender = new MoozicOrb.IO.UserQuery().GetUserById(msg.SenderId);
        //msg.SenderName = sender.FirstName + " " + sender.LastName;

        return Ok(msg);
    }

    // =========================================
    // POST: Send DM
    // =========================================
    [HttpPost]
    public async Task<IActionResult> CreateMessage(
        [FromBody] JsonElement body)
    {
        if (!body.TryGetProperty("receiverId", out var r) ||
            !body.TryGetProperty("text", out var t))
            return BadRequest();

        int senderId = GetUserId();
        int receiverId = r.GetInt32();
        string text = t.GetString();

        var messageId =
            _service.CreateDirectMessage(senderId, receiverId, text);

        // 🔔 Notify receiver
        foreach (var conn in _connections.GetConnections(receiverId))
        {
            await _hub.Clients.Client(conn)
                .SendAsync("OnDirectMessage", new
                {
                    senderId,
                    messageId
                });
        }

        // 🔔 Notify sender (multi-tab support)
        foreach (var conn in _connections.GetConnections(senderId))
        {
            await _hub.Clients.Client(conn)
                .SendAsync("OnDirectMessage", new
                {
                    senderId,
                    messageId
                });
        }

        return Ok(new { messageId });
    }

    // =========================================
    // GET: Bootstrap — ALL direct messages
    // Used once at login
    // =========================================
    [HttpGet]
    public ActionResult GetAllDirectMessages()
    {
        int me = GetUserId();

        // Returns:
        // Dictionary<int, List<DirectMessageDto>>
        var conversations = _service.GetAllDirectMessages(me);

        return Ok(new
        {
            users = conversations.Keys,
            messages = conversations
        });
    }
}




