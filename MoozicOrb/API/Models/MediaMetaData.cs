namespace MoozicOrb.API.Models
{
    public class MediaMetadata
    {
        public string RelativePath { get; set; }     // "MoozicOrb/media/Audio/guid.mp3"
        public string SnippetPath { get; set; }      // "MoozicOrb/media/Audio/guid_snippet.mp3"
        public int DurationSeconds { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}