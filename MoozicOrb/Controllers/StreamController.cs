using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MoozicOrb.Hubs;
using MoozicOrb.Services;
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
    public async Task<IActionResult> StartStream([FromForm] string sessionId)
    {
        // Resolve session → user
        var session = SessionStore.GetSession(sessionId);
        if (session == null)
            return Unauthorized("Invalid session");

        int userId = session.UserId;
        string streamId = Guid.NewGuid().ToString("N");

        // Notify listeners (if any already joined)
        await _hub.Clients.Group($"stream_{streamId}")
            .SendAsync("StreamStarted", new
            {
                streamId,
                startedBy = userId,
                timestamp = DateTime.UtcNow
            });

        return Ok(new { streamId });
    }

    [HttpPost("stop/{streamId}")]
    public async Task<IActionResult> StopStream(string streamId, [FromForm] string sessionId)
    {
        var session = SessionStore.GetSession(sessionId);
        if (session == null)
            return Unauthorized("Invalid session");

        int userId = session.UserId;

        await _hub.Clients.Group($"stream_{streamId}")
            .SendAsync("StreamStopped", new
            {
                streamId,
                stoppedBy = userId,
                timestamp = DateTime.UtcNow
            });

        return Ok();
    }
}



