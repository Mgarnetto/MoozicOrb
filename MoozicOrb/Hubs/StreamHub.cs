using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace MoozicOrb.Hubs
{
    public class StreamHub : Hub
    {
        // streamId -> broadcaster connectionId
        private static readonly ConcurrentDictionary<string, string> Broadcasters = new();

        public async Task JoinStream(string streamId, string role)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"stream:{streamId}");

            if (role == "broadcaster")
            {
                Broadcasters[streamId] = Context.ConnectionId;

                // Notify listeners broadcaster is ready
                await Clients.Group($"stream:{streamId}")
                    .SendAsync("BroadcasterReady");
            }
            else
            {
                // Listener joined — if broadcaster exists, tell them
                if (Broadcasters.ContainsKey(streamId))
                {
                    await Clients.Caller.SendAsync("BroadcasterReady");
                }
            }
        }

        public async Task SendOffer(string streamId, object offer)
        {
            if (Broadcasters.TryGetValue(streamId, out var broadcasterId))
            {
                // send offer to everyone except broadcaster
                await Clients.GroupExcept($"stream:{streamId}", broadcasterId)
                    .SendAsync("ReceiveOffer", offer);
            }
        }

        public async Task SendAnswer(string streamId, object answer)
        {
            if (Broadcasters.TryGetValue(streamId, out var broadcasterId))
            {
                await Clients.Client(broadcasterId)
                    .SendAsync("ReceiveAnswer", answer);
            }
        }

        public async Task SendIceCandidate(string streamId, object candidate)
        {
            await Clients.Group($"stream:{streamId}")
                .SendAsync("ReceiveIceCandidate", candidate);
        }

        public override async Task OnDisconnectedAsync(Exception ex)
        {
            foreach (var kvp in Broadcasters)
            {
                if (kvp.Value == Context.ConnectionId)
                {
                    Broadcasters.TryRemove(kvp.Key, out _);
                    await Clients.Group($"stream:{kvp.Key}")
                        .SendAsync("BroadcasterDisconnected");
                }
            }

            await base.OnDisconnectedAsync(ex);
        }
    }
}

