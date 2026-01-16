using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace MoozicOrb.Hubs
{
    public class StreamHub : Hub
    {
        // streamId → broadcaster connectionId
        private static readonly ConcurrentDictionary<string, string> _broadcasters = new();

        // connectionId → streamId
        private static readonly ConcurrentDictionary<string, string> _connections = new();

        // -----------------------------
        // JOIN STREAM
        // -----------------------------
        public async Task JoinStream(string streamId, string role)
        {
            if (string.IsNullOrWhiteSpace(streamId))
                throw new HubException("Invalid streamId");

            string groupName = GetGroup(streamId);

            _connections[Context.ConnectionId] = streamId;
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            if (role == "broadcaster")
            {
                // Enforce single broadcaster
                if (_broadcasters.ContainsKey(streamId))
                    throw new HubException("Stream already has a broadcaster");

                _broadcasters[streamId] = Context.ConnectionId;

                // Notify listeners that broadcaster is ready
                await Clients.Group(groupName)
                    .SendAsync("BroadcasterReady");
            }
        }

        // -----------------------------
        // WEBRTC SIGNALING
        // -----------------------------
        public async Task SendOffer(string streamId, object offer)
        {
            if (!_broadcasters.TryGetValue(streamId, out var broadcasterConn))
                throw new HubException("No broadcaster");

            // Broadcaster → listeners
            if (Context.ConnectionId != broadcasterConn)
                throw new HubException("Only broadcaster can send offer");

            await Clients.Group(GetGroup(streamId))
                .SendAsync("ReceiveOffer", offer);
        }

        public async Task SendAnswer(string streamId, object answer)
        {
            if (!_broadcasters.TryGetValue(streamId, out var broadcasterConn))
                throw new HubException("No broadcaster");

            // Listener → broadcaster
            await Clients.Client(broadcasterConn)
                .SendAsync("ReceiveAnswer", answer);
        }

        public async Task SendIceCandidate(string streamId, object candidate)
        {
            if (!_broadcasters.TryGetValue(streamId, out var broadcasterConn))
                return;

            // Broadcaster → listeners
            if (Context.ConnectionId == broadcasterConn)
            {
                await Clients.Group(GetGroup(streamId))
                    .SendAsync("ReceiveIceCandidate", candidate);
            }
            // Listener → broadcaster
            else
            {
                await Clients.Client(broadcasterConn)
                    .SendAsync("ReceiveIceCandidate", candidate);
            }
        }

        // -----------------------------
        // DISCONNECT CLEANUP
        // -----------------------------
        public override async Task OnDisconnectedAsync(System.Exception exception)
        {
            if (_connections.TryRemove(Context.ConnectionId, out var streamId))
            {
                string group = GetGroup(streamId);

                // If broadcaster left → stream dead
                if (_broadcasters.TryGetValue(streamId, out var broadcasterConn) &&
                    broadcasterConn == Context.ConnectionId)
                {
                    _broadcasters.TryRemove(streamId, out _);

                    await Clients.Group(group)
                        .SendAsync("StreamEnded");
                }

                await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
            }

            await base.OnDisconnectedAsync(exception);
        }

        // -----------------------------
        // HELPERS
        // -----------------------------
        private static string GetGroup(string streamId)
            => $"stream_{streamId}";
    }
}


