using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MoozicOrb.Hubs;

namespace MoozicOrb.Controllers;

[ApiController]
[Route("api/stream")]
public class StreamController : ControllerBase
{
    private readonly IHubContext<StreamHub> _hub;

    // TEMP — hardcoded until auth lands
    private const int USER_ID = 1;

    public StreamController(IHubContext<StreamHub> hub)
    {
        _hub = hub;
    }

    [HttpPost("start/{streamId}")]
    public async Task<IActionResult> StartStream(string streamId)
    {
        await _hub.Clients.Group($"stream:{streamId}")
            .SendAsync("StreamStarted", new
            {
                streamId,
                startedBy = USER_ID,
                timestamp = DateTime.UtcNow
            });

        return Ok();
    }

    [HttpPost("stop/{streamId}")]
    public async Task<IActionResult> StopStream(string streamId)
    {
        await _hub.Clients.Group($"stream:{streamId}")
            .SendAsync("StreamStopped", new
            {
                streamId,
                stoppedBy = USER_ID,
                timestamp = DateTime.UtcNow
            });

        return Ok();
    }
}

