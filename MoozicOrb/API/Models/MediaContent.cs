namespace MoozicOrb.API.Models
{
    public class MediaContent
    {
        public long MediaContentId { get; set; }
        public long PostId { get; set; } // post where media was uploaded
        public string FilePath { get; set; } // relative path to file (server assumed)
        public string MediaContentType { get; set; } // e.g., "image/png", "video/mp4"
        public int Width { get; set; } // width of media
        public int Height { get; set; } // height of media
        public DateTime TimeStamp { get; set; }
    }
}
