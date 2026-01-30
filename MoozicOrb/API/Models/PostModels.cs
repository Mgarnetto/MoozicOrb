using System;
using System.Collections.Generic;

namespace MoozicOrb.API.Models
{
    // --- POSTS ---
    public class CreatePostRequest
    {
        public string Content { get; set; }
        public int Type { get; set; } // 1=Status, 2=Article, 3=Classified
        public List<MediaAttachmentDto> MediaAttachments { get; set; }
    }

    public class PostDto
    {
        public long PostId { get; set; }
        public int UserId { get; set; }
        public string Content { get; set; }
        public int Type { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<MediaAttachmentDto> Attachments { get; set; }
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
