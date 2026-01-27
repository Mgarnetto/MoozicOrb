using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace MoozicOrb.Hubs
{
    public class TestStreamHub : Hub
    {
        // Optional: Send "Now Playing" metadata when a user connects
        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
            await Clients.Caller.SendAsync("SystemMessage", "Connected to MoozicOrb Radio.");
        }
    }
}