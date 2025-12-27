namespace MoozicOrb.Api.Models
{
    public class MessageDto
    {
        public long MessageId { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; }   // hydrate later
        public string AvatarUrl { get; set; }    // hydrate later
        public string Text { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
