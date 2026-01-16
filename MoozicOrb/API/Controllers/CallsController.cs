using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MoozicOrb.Hubs;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace MoozicOrb.Api.Controllers
{
    [ApiController]
    [Route("api/calls")]
    public class CallsController : ControllerBase
    {
        private readonly IHubContext<CallHub> _callHub;
        private const int USER_ID = 1; // TEMP until auth

        public CallsController(IHubContext<CallHub> callHub)
        {
            _callHub = callHub;
        }

        // --------------------------
        // START CALL
        // --------------------------
        [HttpPost("start")]
        public async Task<IActionResult> StartCall([FromBody] StartCallDto dto)
        {
            if (dto == null || dto.CalleeUserId <= 0)
                return BadRequest();

            string callId = System.DateTime.UtcNow.Ticks.ToString();

            await _callHub.Clients.User(dto.CalleeUserId.ToString())
                .SendAsync("IncomingCall", new
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
            await _callHub.Clients.User(dto.CallerUserId.ToString())
                .SendAsync("CallAccepted", new { callId = dto.CallId });

            return Ok();
        }

        // --------------------------
        // REJECT CALL
        // --------------------------
        [HttpPost("reject")]
        public async Task<IActionResult> RejectCall([FromBody] CallActionDto dto)
        {
            await _callHub.Clients.User(dto.CallerUserId.ToString())
                .SendAsync("CallRejected", new { callId = dto.CallId });

            return Ok();
        }
    }

    

    public class StartCallDto
    {
        [Required]
        public int CalleeUserId { get; set; }

        public string Type { get; set; }
    }

    public class CallActionDto
    {
        public string CallId { get; set; }
        public int CallerUserId { get; set; }
    }
}
