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
        public long Id { get; set; }              // Unique Database ID of the post
        public int AuthorId { get; set; }         // ID of the User who created the post
        public string AuthorName { get; set; }    // Display Name of the author (for UI)
        public string AuthorPic { get; set; }     // URL to author's avatar (for UI)

        // --- Context (Where does this post live?) ---
        // ContextType examples: "user", "group", "state", "page"
        public string ContextType { get; set; }
        // ContextId examples: "105" (UserId), "55" (GroupId), "GA" (StateCode)
        public string ContextId { get; set; }

        // --- Content Data ---
        // Type examples: "standard" (text status), "classified" (for sale), "article", "tutorial"
        // This tells the UI which ViewComponent or template to use to render the post.
        public string Type { get; set; }

        public string Title { get; set; }         // Optional headline (used for Articles/Classifieds)
        public string Text { get; set; }          // Main body content (HTML or Plain text)
        public string ImageUrl { get; set; }      // Main "Cover Image" for the post (separate from Gallery)

        public DateTime CreatedAt { get; set; }   // UTC Timestamp
        public string CreatedAgo { get; set; }    // Pre-calculated "Time Ago" string (e.g., "5m ago")

        // --- Polymorphic Extras (Specific to certain Post Types) ---
        // These are nullable because a standard status update won't use them.

        public decimal? Price { get; set; }       // Used only if Type == "classified" or "store_item"
        public string LocationLabel { get; set; } // Used for Classifieds or Events (e.g., "Atlanta, GA")
        public string DifficultyLevel { get; set; }// Used only if Type == "tutorial" (e.g., "Beginner")
        public string VideoUrl { get; set; }      // External link (YouTube/Vimeo) if this is a video post
        public long? MediaId { get; set; }        // ID for a SINGLE audio track attachment (Legacy/Simple Audio)
        public string Category { get; set; }      // Tagging/Filtering (e.g., "Rock", "Equipment", "News")

        // --- The Gallery ---
        // A list of multiple media items attached to this post (Photos, Videos, Songs).
        // This is populated by looking up the 'post_media' table.
        public List<MediaAttachmentDto> Attachments { get; set; } = new List<MediaAttachmentDto>();
    }

    /// <summary>
    /// Represents a single item inside a Post's gallery (e.g., one photo in a carousel).
    /// </summary>
    public class MediaAttachmentDto
    {
        public long MediaId { get; set; }   // ID of the media record
        public int MediaType { get; set; }  // Enum Code: 1=Audio, 2=Video, 3=Image
        public string Url { get; set; }     // The full URL to the file (populated during GET)
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
}