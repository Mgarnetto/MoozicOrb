namespace MoozicOrb.API.Models
{
    public class StreamInfoDto
    {
        public long TrackId { get; set; }        // Changed from string to long
        public string PhysicalPath { get; set; }
        public string MimeType { get; set; } = "audio/mpeg";

        public int UploadedByUserId { get; set; }
        public int Visibility { get; set; }
        public bool IsUploaderBanned { get; set; }
    }
}