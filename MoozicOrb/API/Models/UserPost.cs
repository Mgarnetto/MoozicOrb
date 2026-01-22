namespace MoozicOrb.API.Models
{
    public class UserPost
    {
        public long PostId { get; set; }
        public int UserId { get; set; }
        public long MediaContentId { get; set; } // will represent the media content id associated
        // will all media uploaded with this post. 
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }

    }
}
