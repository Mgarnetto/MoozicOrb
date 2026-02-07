using System;

namespace MoozicOrb.API.Models
{
    public class NotificationDto
    {
        public long Id { get; set; }
        public int ActorId { get; set; }
        public string ActorName { get; set; }
        public string ActorPic { get; set; }
        public string Type { get; set; }      // "message", "post", "like"
        public string Message { get; set; }   // "sent a message"
        public long ReferenceId { get; set; } // ID to link to
        public bool IsRead { get; set; }
        public string CreatedAgo { get; set; }
    }
}