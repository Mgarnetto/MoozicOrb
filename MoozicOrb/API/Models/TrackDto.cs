namespace MoozicOrb.Api.Models
{
    public class TrackDto
    {
        public long TrackId { get; set; }        // Changed from string to long
        public string Title { get; set; }
        public string Artist { get; set; }
        public int DurationSeconds { get; set; }
        public int UploadedByUserId { get; set; }
        public bool IsPublic { get; set; }
    }
}