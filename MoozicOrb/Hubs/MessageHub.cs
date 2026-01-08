using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MoozicOrb.Hubs
{
    public class MessageHub : Hub
    {
        // -----------------------------
        // TEMP USER (until auth wired)
        // -----------------------------
        private int GetUserId()
        {
            return 1; // TODO: replace with real auth
        }

        // ------------------------------------------------
        // USER ↔ CONNECTION TRACKING (RTC NEEDS THIS)
        // ------------------------------------------------
        private static readonly ConcurrentDictionary<int, HashSet<string>>
            _userConnections = new();

        // ------------------------------------------------
        // CONNECTION LIFECYCLE
        // ------------------------------------------------
        public override async Task OnConnectedAsync()
        {
            int userId = GetUserId();
            var connections = _userConnections.GetOrAdd(userId, _ => new HashSet<string>());
            lock (connections) connections.Add(Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(System.Exception ex)
        {
            int userId = GetUserId();
            if (_userConnections.TryGetValue(userId, out var connections))
            {
                lock (connections)
                {
                    connections.Remove(Context.ConnectionId);
                    if (connections.Count == 0)
                        _userConnections.TryRemove(userId, out _);
                }
            }
            await base.OnDisconnectedAsync(ex);
        }

        // ------------------------------------------------
        // GROUP CHAT (DO NOT BREAK)
        // ------------------------------------------------
        public async Task JoinGroup(long groupId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"group-{groupId}");
        }

        public async Task SendMessage(long groupId, string message)
        {
            await Clients.Group($"group-{groupId}")
                .SendAsync("ReceiveMessage", new
                {
                    userId = GetUserId(),
                    message
                });
        }

        // ------------------------------------------------
        // 🔊 WEBRTC P2P SIGNALING (Audio/Video Calls)
        // ------------------------------------------------
        public async Task SendRtcOffer(long callId, string sdp, int toUserId)
        {
            await SendToUser(toUserId, "RtcOffer", new
            {
                fromUserId = GetUserId(),
                sdp,
                callId
            });
        }

        public async Task SendRtcAnswer(long callId, string sdp, int toUserId)
        {
            await SendToUser(toUserId, "RtcAnswer", new
            {
                fromUserId = GetUserId(),
                sdp,
                callId
            });
        }

        public async Task SendRtcIceCandidate(int toUserId, object candidate)
        {
            await SendToUser(toUserId, "RtcIceCandidate", new
            {
                fromUserId = GetUserId(),
                candidate
            });
        }

        public async Task SendRtcHangup(int toUserId)
        {
            await SendToUser(toUserId, "RtcHangup", new { fromUserId = GetUserId() });
        }

        // ------------------------------------------------
        // STREAM SIGNALING
        // ------------------------------------------------
        public async Task RequestStreamJoin(long streamId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"stream-{streamId}");
        }

        public async Task SendStreamAnswer(long streamId, string sdp)
        {
            await Clients.Group($"stream-{streamId}").SendAsync("StreamAnswer", new
            {
                userId = GetUserId(),
                streamId,
                sdp
            });
        }

        public async Task SendStreamIceCandidate(long streamId, object candidate)
        {
            await Clients.Group($"stream-{streamId}").SendAsync("StreamIceCandidate", new
            {
                userId = GetUserId(),
                streamId,
                candidate
            });
        }

        // ------------------------------------------------
        // HELPER: SEND TO USER
        // ------------------------------------------------
        private async Task SendToUser(int userId, string method, object payload)
        {
            if (!_userConnections.TryGetValue(userId, out var connections)) return;
            foreach (var connId in connections)
            {
                await Clients.Client(connId).SendAsync(method, payload);
            }
        }
    }
}




