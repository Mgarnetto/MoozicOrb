using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MoozicOrb.API.Models; // Need this to see MediaAttachmentDto

namespace MoozicOrb.API.Models
{
    public class CreatePostDto
    {
        // Context
        [Required] public string ContextType { get; set; }
        [Required] public string ContextId { get; set; }

        // Core
        [Required] public string Type { get; set; }
        public string Title { get; set; }
        public string Text { get; set; }
        public string ImageUrl { get; set; } // Cover image

        // Polymorphic Extras
        public decimal? Price { get; set; }
        public string LocationLabel { get; set; }
        public string DifficultyLevel { get; set; }
        public string VideoUrl { get; set; }

        // Use this for a SINGLE main attachment (legacy or simple posts)
        public int? MediaId { get; set; }
        public string Category { get; set; }

        // --- THE MISSING PIECE ---
        // This accepts the list of IDs you got back from the Upload Controller.
        // Example: [{ MediaId: 55, MediaType: 3 }, { MediaId: 56, MediaType: 3 }]
        public List<MediaAttachmentDto> MediaAttachments { get; set; } = new List<MediaAttachmentDto>();
    }
}