using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace MoozicOrb.Hubs
{
    public class StreamHub : Hub
    {
        private const int USER_ID = 1; // dummy user

        public StreamHub() { }

        // Minimal JoinStream for testing
        public async Task JoinStream(string streamId)
        {
            try
            {
                if (string.IsNullOrEmpty(streamId))
                    throw new System.ArgumentException("streamId cannot be null or empty");

                // Add to SignalR group (does not fail)
                await Groups.AddToGroupAsync(Context.ConnectionId, $"stream:{streamId}");

                // Notify the caller that they joined successfully
                await Clients.Caller.SendAsync("JoinedStream", new { streamId, userId = USER_ID });

                // Optional: log
                System.Console.WriteLine($"User {USER_ID} joined stream {streamId}");
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"Error in JoinStream: {ex.Message}");
                throw; // Let SignalR report error to JS
            }
        }

        public override async Task OnDisconnectedAsync(System.Exception exception)
        {
            // Nothing risky here, safe
            System.Console.WriteLine($"Connection {Context.ConnectionId} disconnected");
            await base.OnDisconnectedAsync(exception);
        }
    }
}

