namespace MoozicOrb.Api.Models
{
    public class DirectMessage
    {
        public long MessageId { get; set; }
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public string MessageText { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
