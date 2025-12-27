namespace MoozicOrb.Api.Models
{
    public class GroupMessageDto
    {
        public long MessageId { get; set; }
        public long GroupId { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; }
        public string SenderProfilePicUrl { get; set; }
        public string Text { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
