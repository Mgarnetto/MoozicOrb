using Microsoft.AspNetCore.SignalR;
using MoozicOrb.Services;
using System;
using System.Threading.Tasks;

namespace MoozicOrb.Hubs
{
    public class PostHub : Hub
    {
        private readonly UserConnectionManager _connections;

        public PostHub(UserConnectionManager connections)
        {
            _connections = connections;
        }

        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }

        // Called by Client to subscribe to a page feed (e.g. "user_105")
        public async Task JoinGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        public async Task LeaveGroup(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }

        // Optional: Keep for user tracking if needed later
        public Task AttachUserSession(int userId)
        {
            _connections.AddConnection(userId, Context.ConnectionId);
            return Task.CompletedTask;
        }
    }
}