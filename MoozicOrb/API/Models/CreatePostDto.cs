using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MoozicOrb.API.Models
{
    public class CreatePostDto
    {
        // Context (Required)
        [Required] public string ContextType { get; set; }
        [Required] public string ContextId { get; set; }
        [Required] public string Type { get; set; }

        // Optional Core Data (Must be nullable '?' to be optional)
        public string? Title { get; set; }
        public string? Text { get; set; }
        public string? ImageUrl { get; set; }

        // Polymorphic Extras (Must be nullable '?')
        public decimal? Price { get; set; }
        public string? LocationLabel { get; set; }
        public string? DifficultyLevel { get; set; }
        public string? VideoUrl { get; set; }

        public int? MediaId { get; set; }
        public string? Category { get; set; }

        public List<MediaAttachmentDto> MediaAttachments { get; set; } = new List<MediaAttachmentDto>();
    }
}