using Microsoft.AspNetCore.SignalR;
using MoozicOrb.Services;
using System;
using System.Threading.Tasks;

namespace MoozicOrb.Hubs
{
    public class MessageHub : Hub
    {
        private readonly UserConnectionManager _connections;

        public MessageHub(UserConnectionManager connections)
        {
            _connections = connections;
        }

        // Called AFTER login
        public Task AttachUserSession(int userId)
        {
            var session = SessionStore.GetSessionByUserId(userId);
            if (session == null)
                throw new HubException("Invalid session");

            Context.Items["UserId"] = userId;

            _connections.AddConnection(userId, Context.ConnectionId);

            return Task.CompletedTask;
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            if (Context.Items.TryGetValue("UserId", out var val)
                && val is int userId)
            {
                _connections.RemoveConnection(userId, Context.ConnectionId);
            }

            return base.OnDisconnectedAsync(exception);
        }

        // Group chat support
        public async Task JoinGroup(long groupId)
        {
            if (!Context.Items.ContainsKey("UserId"))
                throw new HubException("Not authenticated");

            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                $"group-{groupId}"
            );
        }
    }
}









