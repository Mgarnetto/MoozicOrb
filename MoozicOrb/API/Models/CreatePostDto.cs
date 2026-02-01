using System.ComponentModel.DataAnnotations;

namespace MoozicOrb.API.Models
{
    public class CreatePostDto
    {
        // Context
        [Required] public string ContextType { get; set; }
        [Required] public string ContextId { get; set; }

        // Core
        [Required] public string Type { get; set; } // "status", "classified", etc.
        public string Title { get; set; }
        public string Text { get; set; }
        public string ImageUrl { get; set; }

        // Polymorphic
        public decimal? Price { get; set; }
        public string LocationLabel { get; set; }
        public string DifficultyLevel { get; set; }
        public string VideoUrl { get; set; }
        public int? MediaId { get; set; }
        public string Category { get; set; }
    }
}