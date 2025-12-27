namespace MoozicOrb.Models;

public class GroupMessage
{
    public long MessageId { get; set; }
    public long GroupId { get; set; }
    public int SenderId { get; set; }
    public string MessageText { get; set; } = "";
    public bool MessageDeleted { get; set; }
    public DateTime Timestamp { get; set; }
}
