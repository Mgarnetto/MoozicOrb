using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MoozicOrb.Hubs
{
    public class MessageHub : Hub
    {
        // -----------------------------
        // USER ↔ CONNECTION TRACKING
        // -----------------------------
        private static readonly ConcurrentDictionary<int, HashSet<string>> _userConnections = new();

        // -----------------------------
        // CONNECTION LIFECYCLE
        // -----------------------------
        public override async Task OnConnectedAsync()
        {
            // Do NOT assign dummy user here anymore.
            // AttachUserSession will be called after login from JS
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(System.Exception ex)
        {
            if (Context.Items.TryGetValue("UserId", out var uidObj) && uidObj is int userId)
            {
                if (_userConnections.TryGetValue(userId, out var connections))
                {
                    lock (connections)
                    {
                        connections.Remove(Context.ConnectionId);
                        if (connections.Count == 0)
                            _userConnections.TryRemove(userId, out _);
                    }
                }
            }

            await base.OnDisconnectedAsync(ex);
        }

        // -----------------------------
        // USER SESSION ATTACHMENT
        // -----------------------------
        public async Task AttachUserSession(int userId)
        {
            // Store connection for this user
            var connections = _userConnections.GetOrAdd(userId, _ => new HashSet<string>());
            lock (connections)
                connections.Add(Context.ConnectionId);

            // Store userId in Context for authorization checks
            Context.Items["UserId"] = userId;
        }

        // -----------------------------
        // GROUP CHAT
        // -----------------------------
        public async Task JoinGroup(long groupId)
        {
            if (!Context.Items.TryGetValue("UserId", out var _))
                throw new HubException("Not logged in");

            await Groups.AddToGroupAsync(Context.ConnectionId, $"group-{groupId}");
        }

        // -----------------------------
        // SESSION REHYDRATION / GROUP JOIN
        // -----------------------------
        public async Task JoinGroups(IEnumerable<long> groupIds)
        {
            if (!Context.Items.TryGetValue("UserId", out var _))
                throw new HubException("Not logged in");

            foreach (var groupId in groupIds)
            {
                await Groups.AddToGroupAsync(
                    Context.ConnectionId,
                    $"group-{groupId}"
                );
            }
        }

        public async Task SendMessage(long groupId, string message)
        {
            if (!Context.Items.TryGetValue("UserId", out var uidObj) || uidObj is not int userId)
                throw new HubException("Not logged in");

            await Clients.Group($"group-{groupId}")
                .SendAsync("ReceiveMessage", new
                {
                    userId,
                    message
                });
        }

        // -----------------------------
        // 🔊 WEBRTC P2P SIGNALING (Audio/Video Calls)
        // -----------------------------
        public async Task SendRtcOffer(long callId, string sdp, int toUserId)
        {
            await SendToUser(toUserId, "RtcOffer", new
            {
                fromUserId = GetUserIdOrThrow(),
                sdp,
                callId
            });
        }

        public async Task SendRtcAnswer(long callId, string sdp, int toUserId)
        {
            await SendToUser(toUserId, "RtcAnswer", new
            {
                fromUserId = GetUserIdOrThrow(),
                sdp,
                callId
            });
        }

        public async Task SendRtcIceCandidate(int toUserId, object candidate)
        {
            await SendToUser(toUserId, "RtcIceCandidate", new
            {
                fromUserId = GetUserIdOrThrow(),
                candidate
            });
        }

        public async Task SendRtcHangup(int toUserId)
        {
            await SendToUser(toUserId, "RtcHangup", new
            {
                fromUserId = GetUserIdOrThrow()
            });
        }

        // -----------------------------
        // STREAM SIGNALING
        // -----------------------------
        public async Task RequestStreamJoin(long streamId)
        {
            if (!Context.Items.TryGetValue("UserId", out var _))
                throw new HubException("Not logged in");

            await Groups.AddToGroupAsync(Context.ConnectionId, $"stream-{streamId}");
        }

        public async Task SendStreamAnswer(long streamId, string sdp)
        {
            await Clients.Group($"stream-{streamId}").SendAsync("StreamAnswer", new
            {
                userId = GetUserIdOrThrow(),
                streamId,
                sdp
            });
        }

        public async Task SendStreamIceCandidate(long streamId, object candidate)
        {
            await Clients.Group($"stream-{streamId}").SendAsync("StreamIceCandidate", new
            {
                userId = GetUserIdOrThrow(),
                streamId,
                candidate
            });
        }

        // -----------------------------
        // HELPER METHODS
        // -----------------------------
        private int GetUserIdOrThrow()
        {
            if (!Context.Items.TryGetValue("UserId", out var uidObj) || uidObj is not int userId)
                throw new HubException("Not logged in");
            return userId;
        }

        private async Task SendToUser(int userId, string method, object payload)
        {
            if (!_userConnections.TryGetValue(userId, out var connections)) return;

            foreach (var connId in connections)
                await Clients.Client(connId).SendAsync(method, payload);
        }
    }
}







