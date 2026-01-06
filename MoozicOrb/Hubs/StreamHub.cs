using Microsoft.AspNetCore.SignalR;
using MoozicOrb.Services.Interfaces;

namespace MoozicOrb.Hubs;

public class StreamHub : Hub
{
    private readonly IStreamSessionService _streamSessions;

    public StreamHub(IStreamSessionService streamSessions)
    {
        _streamSessions = streamSessions;
    }

    public async Task JoinStream(string streamId, int userId, bool isBroadcaster)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, streamId);

        await _streamSessions.RegisterConnectionAsync(
            streamId,
            userId,
            Context.ConnectionId,
            isBroadcaster
        );
    }

    public async Task LeaveStream(string streamId, int userId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, streamId);
        await _streamSessions.RemoveConnectionAsync(streamId, userId);
    }

    public Task Heartbeat(string streamId, int userId)
    {
        return _streamSessions.RefreshHeartbeatAsync(streamId, userId);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await _streamSessions.RemoveConnectionByConnectionIdAsync(
            Context.ConnectionId
        );

        await base.OnDisconnectedAsync(exception);
    }
}
