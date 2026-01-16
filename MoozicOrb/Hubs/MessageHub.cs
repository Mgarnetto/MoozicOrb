using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace MoozicOrb.Hubs
{
    public class MessageHub : Hub
    {
        private static readonly ConcurrentDictionary<int, HashSet<string>> _userConnections = new();

        public Task AttachUserSession(int userId)
        {
            var conns = _userConnections.GetOrAdd(userId, _ => new HashSet<string>());
            lock (conns)
                conns.Add(Context.ConnectionId);

            Context.Items["UserId"] = userId;
            return Task.CompletedTask;
        }

        public async Task JoinGroup(long groupId)
        {
            if (!Context.Items.ContainsKey("UserId"))
                throw new HubException("Not logged in");

            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                $"group-{groupId}"
            );
        }
    }
}








