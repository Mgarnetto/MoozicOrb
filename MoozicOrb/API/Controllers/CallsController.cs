using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MoozicOrb.Hubs;
using System.Threading.Tasks;

namespace MoozicOrb.Api.Controllers
{
    [ApiController]
    [Route("api/calls")]
    public class CallsController : ControllerBase
    {
        private readonly IHubContext<MessageHub> _hub;
        private const int USER_ID = 1; // TEMP until auth

        public CallsController(IHubContext<MessageHub> hub)
        {
            _hub = hub;
        }

        // --------------------------
        // START CALL
        // --------------------------
        [HttpPost("start")]
        public async Task<IActionResult> StartCall([FromBody] StartCallDto dto)
        {
            long callId = System.DateTime.UtcNow.Ticks;
            int calleeId = dto.CalleeUserId;

            // Notify callee via SignalR
            await _hub.Clients.User(calleeId.ToString()).SendAsync("IncomingCall", new
            {
                callId,
                fromUserId = USER_ID,
                type = dto.Type ?? "audio"
            });

            return Ok(new { callId });
        }

        // --------------------------
        // ACCEPT CALL
        // --------------------------
        [HttpPost("accept")]
        public async Task<IActionResult> AcceptCall([FromBody] CallActionDto dto)
        {
            await _hub.Clients.User(dto.CallerUserId.ToString())
                .SendAsync("CallAccepted", new { callId = dto.CallId });
            return Ok();
        }

        // --------------------------
        // REJECT CALL
        // --------------------------
        [HttpPost("reject")]
        public async Task<IActionResult> RejectCall([FromBody] CallActionDto dto)
        {
            await _hub.Clients.User(dto.CallerUserId.ToString())
                .SendAsync("CallRejected", new { callId = dto.CallId });
            return Ok();
        }
    }

    // DTOs
    public class StartCallDto
    {
        public int CalleeUserId { get; set; }
        public string Type { get; set; } // "audio" or "video"
    }

    public class CallActionDto
    {
        public long CallId { get; set; }
        public int CallerUserId { get; set; }
    }
}


