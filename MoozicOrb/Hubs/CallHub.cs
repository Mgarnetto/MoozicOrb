using Microsoft.AspNetCore.SignalR;
using MoozicOrb.Services;
using System;
using System.Threading.Tasks;

namespace MoozicOrb.Hubs
{
    public class CallHub : Hub
    {
        private readonly UserConnectionManager _connections;
        private readonly CallStateService _callState;

        public CallHub(UserConnectionManager connections, CallStateService callState)
        {
            _connections = connections;
            _callState = callState;
        }

        // --- Session Management ---
        public Task AttachUserSession(int userId)
        {
            _connections.AddConnection(userId, Context.ConnectionId);
            Context.Items["UserId"] = userId;
            return Task.CompletedTask;
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            if (Context.Items["UserId"] is int userId)
                _connections.RemoveConnection(userId, Context.ConnectionId);

            return base.OnDisconnectedAsync(exception);
        }

        // --- WebRTC Signaling (Only happens AFTER Accepted) ---
        public async Task RegisterCall(string callId, int calleeUserId)
        {
            // We register state here just to be safe, though Controller usually handles the "Start"
            // This method might be redundant if Controller does the busy logic, 
            // but we keep it for the WebRTC handshake start.
            int callerId = GetUserId();
            _callState.RegisterCall(callId, callerId, calleeUserId);
        }

        public async Task EndCall(string callId)
        {
            var p = _callState.GetCallParticipants(callId);
            if (p == null) return;

            _callState.EndCall(callId);

            // Notify both sides to close connection
            await SendToUser(p.Value.CallerId, "RtcHangup");
            await SendToUser(p.Value.CalleeId, "RtcHangup");
        }

        public async Task SendRtcOffer(string callId, string sdp)
        {
            var p = _callState.GetCallParticipants(callId);
            if (p == null) return;

            // Forward to Callee
            await SendToUser(p.Value.CalleeId, "RtcOffer", new { callId, sdp });
        }

        public async Task SendRtcAnswer(string callId, string sdp)
        {
            var p = _callState.GetCallParticipants(callId);
            if (p == null) return;

            // Forward to Caller
            await SendToUser(p.Value.CallerId, "RtcAnswer", new { sdp });
        }

        public async Task SendRtcIceCandidate(string callId, object candidate)
        {
            var p = _callState.GetCallParticipants(callId);
            if (p == null) return;

            int sender = GetUserId();
            int target = (sender == p.Value.CallerId) ? p.Value.CalleeId : p.Value.CallerId;

            await SendToUser(target, "RtcIceCandidate", new { candidate });
        }

        // --- Helpers ---
        private int GetUserId() => Context.Items["UserId"] is int id ? id : 0;

        private async Task SendToUser(int userId, string method, object? payload = null)
        {
            var connections = _connections.GetConnections(userId);
            foreach (var connId in connections)
            {
                await Clients.Client(connId).SendAsync(method, payload);
            }
        }
    }
}





