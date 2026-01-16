using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace MoozicOrb.Hubs
{
    public class CallHub : Hub
    {
        private static readonly ConcurrentDictionary<int, HashSet<string>> _userConnections = new();
        private static readonly ConcurrentDictionary<string, (int callerId, int calleeId)> _activeCalls = new();

        public Task AttachUserSession(int userId)
        {
            var conns = _userConnections.GetOrAdd(userId, _ => new HashSet<string>());
            lock (conns) conns.Add(Context.ConnectionId);
            Context.Items["UserId"] = userId;
            return Task.CompletedTask;
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            if (Context.Items.TryGetValue("UserId", out var v) && v is int userId &&
                _userConnections.TryGetValue(userId, out var conns))
            {
                lock (conns) conns.Remove(Context.ConnectionId);
                if (conns.Count == 0) _userConnections.TryRemove(userId, out _);
            }
            return base.OnDisconnectedAsync(exception);
        }

        public Task RegisterCall(string callId, int calleeUserId)
        {
            int callerId = GetUserId();
            _activeCalls[callId] = (callerId, calleeUserId);
            return Task.CompletedTask;
        }

        public async Task EndCall(string callId)
        {
            if (!_activeCalls.TryRemove(callId, out var call)) return;

            await SendToUser(call.callerId, "RtcHangup");
            await SendToUser(call.calleeId, "RtcHangup");
        }

        public async Task SendRtcOffer(string callId, string sdp)
        {
            var (caller, callee) = GetCall(callId);
            Ensure(caller);

            await SendToUser(callee, "RtcOffer", new { callId, sdp });
        }

        public async Task SendRtcAnswer(string callId, string sdp)
        {
            var (caller, callee) = GetCall(callId);
            Ensure(callee);

            await SendToUser(caller, "RtcAnswer", new { callId, sdp });
        }

        public async Task SendRtcIceCandidate(string callId, object candidate)
        {
            var (caller, callee) = GetCall(callId);
            int sender = GetUserId();

            int target = sender == caller ? callee : caller;
            await SendToUser(target, "RtcIceCandidate", new { callId, candidate });
        }

        private int GetUserId() =>
            Context.Items["UserId"] is int id ? id : throw new HubException("Not authenticated");

        private (int, int) GetCall(string callId) =>
            _activeCalls.TryGetValue(callId, out var c) ? c : throw new HubException("Call not found");

        private void Ensure(int userId)
        {
            if (GetUserId() != userId)
                throw new HubException("Unauthorized");
        }

        private async Task SendToUser(int userId, string method, object? payload = null)
        {
            if (!_userConnections.TryGetValue(userId, out var conns)) return;
            foreach (var c in conns)
                await Clients.Client(c).SendAsync(method, payload);
        }
    }
}





