using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace MoozicOrb.Hubs
{
    public class GroupHub : Hub
    {
        public Task JoinGroup(long groupId)
        {
            return Groups.AddToGroupAsync(
                Context.ConnectionId,
                $"group-{groupId}"
            );
        }

        public Task LeaveGroup(long groupId)
        {
            return Groups.RemoveFromGroupAsync(
                Context.ConnectionId,
                $"group-{groupId}"
            );
        }
    }
}


