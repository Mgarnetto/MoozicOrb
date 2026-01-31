using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MoozicOrb.Hubs;
using MoozicOrb.Services;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace MoozicOrb.API.Controllers
{
    [ApiController]
    [Route("api/calls")]
    public class CallsController : ControllerBase
    {
        private readonly IHubContext<CallHub> _hub;
        private readonly CallStateService _callState;
        private readonly UserConnectionManager _connections;
        private readonly IHttpContextAccessor _http;

        public CallsController(
            IHubContext<CallHub> hub,
            CallStateService callState,
            UserConnectionManager connections,
            IHttpContextAccessor http)
        {
            _hub = hub;
            _callState = callState;
            _connections = connections;
            _http = http;
        }

        private int GetUserId()
        {
            var sid = _http.HttpContext?.Request.Headers["X-Session-Id"].ToString();
            var session = SessionStore.GetSession(sid); // Assuming SessionStore is available
            if (session == null) throw new System.UnauthorizedAccessException();
            return session.UserId;
        }

        // 1. START CALL (Rings the user)
        [HttpPost("start")]
        public async Task<IActionResult> StartCall([FromBody] StartCallDto dto)
        {
            int callerId = GetUserId();

            if (_callState.IsUserBusy(dto.CalleeUserId))
            {
                return Conflict(new { message = "User is currently in another call." });
            }

            string callId = System.DateTime.UtcNow.Ticks.ToString();

            // Notify Callee
            var calleeConns = _connections.GetConnections(dto.CalleeUserId);

            // FIX: Use !calleeConns.Any() instead of .Count == 0
            if (!calleeConns.Any())
                return NotFound(new { message = "User is offline" });

            foreach (var conn in calleeConns)
            {
                await _hub.Clients.Client(conn).SendAsync("IncomingCall", new
                {
                    callId,
                    fromUserId = callerId,
                    type = dto.Type ?? "audio"
                });
            }

            return Ok(new { callId });
        }

        // 2. ACCEPT CALL (Triggers WebRTC start)
        [HttpPost("accept")]
        public async Task<IActionResult> AcceptCall([FromBody] CallActionDto dto)
        {
            int calleeId = GetUserId();

            // Register the active call state now that it's accepted
            _callState.RegisterCall(dto.CallId, dto.CallerUserId, calleeId);

            // Notify Caller: "They picked up! Start sending WebRTC Offer now."
            var callerConns = _connections.GetConnections(dto.CallerUserId);
            foreach (var conn in callerConns)
            {
                await _hub.Clients.Client(conn).SendAsync("CallAccepted", new { callId = dto.CallId });
            }

            return Ok();
        }

        // 3. REJECT CALL
        [HttpPost("reject")]
        public async Task<IActionResult> RejectCall([FromBody] CallActionDto dto)
        {
            // Notify Caller: "They hung up / Busy"
            var callerConns = _connections.GetConnections(dto.CallerUserId);
            foreach (var conn in callerConns)
            {
                await _hub.Clients.Client(conn).SendAsync("CallRejected", new { callId = dto.CallId });
            }

            return Ok();
        }
    }

    public class StartCallDto
    {
        [Required] public int CalleeUserId { get; set; }
        public string Type { get; set; }
    }

    public class CallActionDto
    {
        public string CallId { get; set; }
        public int CallerUserId { get; set; }
    }
}