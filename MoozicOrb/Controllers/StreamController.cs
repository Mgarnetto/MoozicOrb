using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MoozicOrb.Hubs;
using System;

namespace MoozicOrb.Controllers;

[ApiController]
[Route("api/stream")]
public class StreamController : ControllerBase
{
    private readonly IHubContext<StreamHub> _hub;

    public StreamController(IHubContext<StreamHub> hub)
    {
        _hub = hub;
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartStream()
    {
        int userId = 1; // TEMP: use AppSession in production
        string streamId = Guid.NewGuid().ToString(); // Generate server-side

        // Notify clients (UI update)
        await _hub.Clients.Group($"stream_{userId}_{streamId}")
            .SendAsync("StreamStarted", new { streamId, startedBy = userId, timestamp = DateTime.UtcNow });

        return Ok(new { streamId });
    }

    [HttpPost("stop/{streamId}")]
    public async Task<IActionResult> StopStream(string streamId)
    {
        int userId = 1; // TEMP: use AppSession in production

        await _hub.Clients.Group($"stream_{userId}_{streamId}")
            .SendAsync("StreamStopped", new { streamId, stoppedBy = userId, timestamp = DateTime.UtcNow });

        return Ok();
    }
}


