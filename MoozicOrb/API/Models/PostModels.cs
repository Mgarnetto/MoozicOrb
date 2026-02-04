using System;
using System.Collections.Generic;

namespace MoozicOrb.API.Models
{
    // ==================================================================================
    // POSTS & FEED MODELS
    // Used for transferring post data between the API (PostController) and the UI.
    // ==================================================================================

    /// <summary>
    /// Represents a single post on a feed (Timeline, Group, State Page, etc.).
    /// This is a "Polymorphic" DTO, meaning it holds data for ALL post types 
    /// (Status, Article, Classified, etc.), but some fields will be null depending on the type.
    /// </summary>
    public class PostDto
    {
        // --- Core Identity ---
        public long Id { get; set; }
        public int AuthorId { get; set; }
        public string AuthorName { get; set; }
        public string AuthorPic { get; set; }

        // --- Context ---
        public string ContextType { get; set; }
        public string ContextId { get; set; }

        // --- Content ---
        public string Type { get; set; }
        public string Title { get; set; }
        public string Text { get; set; }
        public string ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedAgo { get; set; }

        // --- Polymorphic Extras ---
        public decimal? Price { get; set; }
        public string LocationLabel { get; set; }
        public string DifficultyLevel { get; set; }
        public string VideoUrl { get; set; }
        public long? MediaId { get; set; }
        public string Category { get; set; }

        // --- NEW: Engagement Data (The missing pieces) ---
        public bool IsLiked { get; set; }      // Did the current user like this?
        public int LikesCount { get; set; }    // Total likes
        public int CommentsCount { get; set; } // Total comments

        public List<MediaAttachmentDto> Attachments { get; set; } = new List<MediaAttachmentDto>();
    }

    /// <summary>
    /// Represents a single item inside a Post's gallery (e.g., one photo in a carousel).
    /// </summary>
    public class MediaAttachmentDto
    {
        public long MediaId { get; set; }   // ID of the media record
        public int MediaType { get; set; }  // Enum Code: 1=Audio, 2=Video, 3=Image
        public string? Url { get; set; }     // The full URL to the file (populated during GET)
    }


    // ==================================================================================
    // COLLECTIONS MODELS
    // Used for creating Albums, Playlists, or Bundles.
    // ==================================================================================

    /// <summary>
    /// Payload sent from Client to create a new Collection (Album/Playlist).
    /// </summary>
    public class CreateCollectionRequest
    {
        public string Title { get; set; }       // Name of the Album/Playlist
        public string Description { get; set; } // Optional blurb
        public int Type { get; set; }           // 1 = Album (released content), 2 = Playlist (curated content)
        public long CoverImageId { get; set; }  // Media ID of the album art (must be uploaded first)

        // The list of songs/videos to include immediately upon creation
        public List<CollectionItemRequest> Items { get; set; }
    }

    /// <summary>
    /// Represents a pointer to a specific piece of media to add to a collection.
    /// </summary>
    public class CollectionItemRequest
    {
        public long TargetId { get; set; }      // The ID of the Media file (Song/Video)
        public int TargetType { get; set; }     // 1=MediaFile (Expandable for other types later)
    }

    /// <summary>
    /// Data sent back to UI to display an Album or Playlist.
    /// </summary>
    public class CollectionDto
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int Type { get; set; }           // 1=Album, 2=Playlist
        public long CoverImageId { get; set; }

        // The actual playable items inside this collection
        public List<CollectionItemDto> Items { get; set; }
    }

    /// <summary>
    /// A resolved item inside a collection, ready for the Player.
    /// </summary>
    public class CollectionItemDto
    {
        public long TargetId { get; set; }      // ID of the media file
        public int TargetType { get; set; }     // Type of the media
        public string Title { get; set; }       // Display Name (e.g., "Song Title")
        public string Url { get; set; }         // Direct stream URL
    }

    public class CommentDto
    {
        public long CommentId { get; set; }
        public long PostId { get; set; }
        public long? ParentId { get; set; }
        public int UserId { get; set; }
        public string AuthorName { get; set; }
        public string AuthorPic { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedAgo { get; set; }
        public List<CommentDto> Replies { get; set; } = new List<CommentDto>();
    }

    public class CreateCommentDto
    {
        public long PostId { get; set; }
        public string Content { get; set; }
        public long? ParentId { get; set; }
    }

    public class LikeDto
    {
        public long PostId { get; set; }
        public bool Liked { get; set; }
    }
}