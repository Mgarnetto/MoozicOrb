using Microsoft.AspNetCore.SignalR;
using MoozicOrb.Services; // UserConnectionManager
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

        // 1. Connection Management (Required for PostController to find users)
        public override Task OnConnectedAsync()
        {
            // We expect the client to identify themselves, similar to MessageHub
            // Or you can rely on Context.UserIdentifier if using JWT auth
            return base.OnConnectedAsync();
        }

        public Task AttachUserSession(int userId)
        {
            // Tracks this specific connection so PostController can target it
            _connections.AddConnection(userId, Context.ConnectionId);
            return Task.CompletedTask;
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            // Clean up when they leave
            // Note: In a real app, you might need to know WHICH userId to remove
            // usually via Context.Items["UserId"] set in AttachUserSession
            return base.OnDisconnectedAsync(exception);
        }

        // 2. Area/Topic Logic (For "State Pages" or specific zones)
        public async Task EnterArea(string areaId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Area_{areaId}");
        }

        public async Task LeaveArea(string areaId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Area_{areaId}");
        }
    }
}