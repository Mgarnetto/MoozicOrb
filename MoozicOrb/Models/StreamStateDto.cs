namespace MoozicOrb.Models
{
    public class StreamStateDto
    {
        public string StreamId { get; set; }
        public bool IsLive { get; set; }
        public int BroadcasterUserId { get; set; }
        public int ListenerCount { get; set; }
        public DateTime LastHeartbeatUtc { get; set; }
    }
}
