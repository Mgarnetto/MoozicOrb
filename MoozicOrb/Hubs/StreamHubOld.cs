using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace MoozicOrb.Hubs;

public class StreamHubOld : Hub
{
    // TEMP: dummy until AppSession wired
    private const int USER_ID = 1;

    public StreamHubOld() { }

    public async Task JoinGroup(string groupName)
    {
        if (string.IsNullOrEmpty(groupName))
            throw new System.ArgumentException("groupName cannot be null or empty");

        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        // Notify caller
        await Clients.Caller.SendAsync("JoinedStream", new { groupName, userId = USER_ID });

        // Optional: log
        System.Console.WriteLine($"User {USER_ID} joined group {groupName}");
    }

    public override async Task OnDisconnectedAsync(System.Exception exception)
    {
        System.Console.WriteLine($"Connection {Context.ConnectionId} disconnected");
        await base.OnDisconnectedAsync(exception);
    }
}


