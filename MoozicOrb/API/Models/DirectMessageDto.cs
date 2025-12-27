namespace MoozicOrb.Api.Models;

public class DirectMessageDto
{
    public long MessageId { get; set; }
    public int SenderId { get; set; }
    public int ReceiverId { get; set; }
    public string SenderName { get; set; }
    public string SenderProfilePicUrl { get; set; }
    public string Text { get; set; }
    public DateTime Timestamp { get; set; }
}
