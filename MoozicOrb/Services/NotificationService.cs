using Microsoft.AspNetCore.SignalR;
using MoozicOrb.API.Models;
using MoozicOrb.Hubs;
using MoozicOrb.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MoozicOrb.Services
{
    public class NotificationService
    {
        private readonly IHubContext<MessageHub> _hub;
        private readonly UserConnectionManager _connections;

        public NotificationService(IHubContext<MessageHub> hub, UserConnectionManager connections)
        {
            _hub = hub;
            _connections = connections;
        }

        // 1. Single User Notification (For Direct Messages)
        public async Task NotifyUser(int recipientId, int actorId, string type, long refId, string customMsg = null)
        {
            if (recipientId == actorId) return;

            string messageText = customMsg ?? GetDefaultMessage(type);

            // A. Save to DB
            var io = new NotificationIO();
            long notifId = io.Insert(recipientId, actorId, type, refId, messageText);

            // B. Send Realtime Alert
            if (notifId > 0)
            {
                var payload = await BuildPayload(notifId, actorId, type, messageText, refId);
                var conns = _connections.GetConnections(recipientId);
                foreach (var cid in conns)
                {
                    await _hub.Clients.Client(cid).SendAsync("OnNotification", payload);
                }
            }
        }

        // 2. Mass Notification (For New Posts -> Followers)
        public async Task NotifyFollowers(int authorId, long postId, string postTitle)
        {
            // A. Get Followers (Using existing IO, or empty list if not ready)
            var followers = new List<int>();
            try
            {
                // Uncomment when ready:
                followers = new GetFollowers().Execute(authorId);
            }
            catch { /* Table might not exist yet */ }

            if (followers.Count == 0) return;

            string msg = $"posted: {postTitle}";
            var io = new NotificationIO();

            // Loop followers (Batching is better for production, but this works for now)
            foreach (int followerId in followers)
            {
                // Save DB
                long notifId = io.Insert(followerId, authorId, "post_new", postId, msg);

                // Send SignalR
                if (notifId > 0)
                {
                    var payload = await BuildPayload(notifId, authorId, "post_new", msg, postId);
                    var conns = _connections.GetConnections(followerId);
                    foreach (var cid in conns)
                    {
                        await _hub.Clients.Client(cid).SendAsync("OnNotification", payload);
                    }
                }
            }
        }

        private string GetDefaultMessage(string type) => type switch
        {
            "message" => "sent you a message",
            "post_new" => "posted something new",
            _ => "updated something"
        };

        private async Task<NotificationDto> BuildPayload(long id, int actorId, string type, string msg, long refId)
        {
            // Lightweight fetch of actor name/pic
            var actor = new UserQuery().GetUserById(actorId);
            return new NotificationDto
            {
                Id = id,
                ActorId = actorId,
                ActorName = actor?.UserName ?? "Someone",
                ActorPic = actor?.ProfilePic ?? "/img/profile_default.jpg",
                Type = type,
                Message = msg,
                ReferenceId = refId
            };
        }
    }
}