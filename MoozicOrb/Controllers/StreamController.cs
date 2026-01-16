using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MoozicOrb.Hubs;
using MoozicOrb.Services;
using System;

namespace MoozicOrb.Controllers;

[ApiController]
[Route("api/streams")]
public class StreamController : ControllerBase
{
    private readonly IHubContext<StreamHub> _hub;

    public StreamController(IHubContext<StreamHub> hub)
    {
        _hub = hub;
    }

    // ----------------------------------------
    // CREATE STREAM SLOT (SERVER-GENERATED ID)
    // ----------------------------------------
    [HttpPost("create")]
    public IActionResult CreateStream([FromForm] string sessionId)
    {
        var session = SessionStore.GetSession(sessionId);
        if (session == null)
            return Unauthorized("Invalid session");

        var streamId = Guid.NewGuid().ToString("N");

        return Ok(new
        {
            streamId,
            ownerUserId = session.UserId,
            createdAt = DateTime.UtcNow
        });
    }

    // ----------------------------------------
    // END STREAM
    // ----------------------------------------
    [HttpPost("{streamId}/end")]
    public async Task<IActionResult> EndStream(
        string streamId,
        [FromForm] string sessionId)
    {
        var session = SessionStore.GetSession(sessionId);
        if (session == null)
            return Unauthorized("Invalid session");

        await _hub.Clients.Group($"stream_{streamId}")
            .SendAsync("StreamEnded", new
            {
                streamId,
                endedBy = session.UserId,
                timestamp = DateTime.UtcNow
            });

        return Ok();
    }
}






