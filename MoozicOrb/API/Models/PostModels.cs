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

            // Context
            public string ContextType { get; set; }
            public string ContextId { get; set; }

            // Content
            public string Type { get; set; }
            public string Title { get; set; }
            public string Text { get; set; }
            public string ImageUrl { get; set; } // Cover Image
            public DateTime CreatedAt { get; set; }
            public string CreatedAgo { get; set; }

            // Polymorphic Extras
            public decimal? Price { get; set; }
            public string LocationLabel { get; set; }
            public string DifficultyLevel { get; set; }
            public string VideoUrl { get; set; }    // External Video
            public long? MediaId { get; set; }      // Single Audio Attachment
            public string Category { get; set; }

            // --- THE GALLERY (CRITICAL FIX) ---
            public List<MediaAttachmentDto> Attachments { get; set; } = new List<MediaAttachmentDto>();
        }

        // --- SHARED ---
        public class MediaAttachmentDto
        {
            public long MediaId { get; set; }
            public int MediaType { get; set; } // 1=Audio, 2=Video, 3=Image
            public string Url { get; set; }    // We will populate this in the GET
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
}