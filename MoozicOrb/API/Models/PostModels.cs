using System;
using System.Collections.Generic;

namespace MoozicOrb.API.Models
{
    // --- POSTS ---
    public class PostDto
    {
        public long Id { get; set; }
        public int AuthorId { get; set; }
        public string AuthorName { get; set; }
        public string AuthorPic { get; set; }

        // Context (Where was this posted?)
        public string ContextType { get; set; } // "loc", "user", "page"
        public string ContextId { get; set; }

        // Core Content
        public string Type { get; set; }        // "status", "article", "classified", "media"
        public string Title { get; set; }
        public string Text { get; set; }        // The main content
        public string ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedAgo { get; set; }  // Formatted string (e.g., "2h ago")

        // Polymorphic Extras (Nullable)
        public decimal? Price { get; set; }             // Classifieds
        public string LocationLabel { get; set; }       // Classifieds
        public string DifficultyLevel { get; set; }     // Tutorials
        public string VideoUrl { get; set; }            // Tutorials/Media
        public long? MediaId { get; set; }              // Single Attached Song ID
        public string Category { get; set; }            // Generic Category

        // Gallery / Multi-Media
        public List<MediaAttachmentDto> Attachments { get; set; } = new List<MediaAttachmentDto>();
    }

    // --- COLLECTIONS ---
    public class CreateCollectionRequest
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public int Type { get; set; } // 1=Album, 2=Playlist
        public long CoverImageId { get; set; }
        public List<CollectionItemRequest> Items { get; set; }
    }

    public class CollectionItemRequest
    {
        public long TargetId { get; set; }
        public int TargetType { get; set; }
    }

    public class CollectionDto
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int Type { get; set; }
        public long CoverImageId { get; set; }
        public List<CollectionItemDto> Items { get; set; }
    }

    public class CollectionItemDto
    {
        public long TargetId { get; set; }
        public int TargetType { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
    }

    // --- SHARED ---
    public class MediaAttachmentDto
    {
        public long MediaId { get; set; }
        public int MediaType { get; set; } // 1=Audio, 2=Video, 3=Image
        public string Url { get; set; }
    }
}