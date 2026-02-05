using MoozicOrb.API.Models;
using System.Collections.Generic;

namespace MoozicOrb.Models
{
    // ==========================================
    // 1. BASE VIEW MODEL
    // ==========================================
    // Every page that needs realtime updates inherits this
    public class BaseSignalRViewModel
    {
        public string SignalRGroup { get; set; } = "global";
    }

    // ==========================================
    // 2. PAGE SPECIFIC MODELS
    // ==========================================

    public class HomeViewModel : BaseSignalRViewModel
    {
    }

    public class LocationViewModel : BaseSignalRViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<ArtistDto> TopArtists { get; set; } = new();
    }

    public class CreatorViewModel : BaseSignalRViewModel
    {
        public int UserId { get; set; }
        public string DisplayName { get; set; }
        public string UserName { get; set; } // Added for URL generation
        public string ProfilePic { get; set; }
        public string CoverImage { get; set; }
        public string Bio { get; set; }

        // --- NEW FIELDS FOR LOGIC ---
        public bool IsCurrentUser { get; set; } // True if viewing own profile
        public List<string> LayoutOrder { get; set; } = new List<string>(); // ["posts", "music", "store"]

        public List<CollectionDto> Collections { get; set; } = new();
    }

    public class PageViewModel : BaseSignalRViewModel
    {
        public string Title { get; set; }
    }

    public class RadioViewModel : BaseSignalRViewModel
    {
        public SongDto CurrentSong { get; set; }
        public List<SongDto> History { get; set; } = new();
    }

    public class GenresViewModel : BaseSignalRViewModel
    {
        public List<GenreDto> Genres { get; set; } = new();
    }

    // ==========================================
    // 3. SETTINGS MODELS
    // ==========================================

    public class AccountSettingsViewModel
    {
        public string DisplayName { get; set; }
        public string Email { get; set; }
    }

    public class PageSettingsViewModel
    {
        public string Bio { get; set; }
        public string CoverImage { get; set; }
        public string BookingEmail { get; set; }

        // Add if you want to support layout saving in the same model
        public List<string> LayoutOrder { get; set; }
    }

    // ==========================================
    // 4. DATA TRANSFER OBJECTS (DTOs)
    // ==========================================
    // These represent the small chunks of data inside lists



    public class ArtistDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Pic { get; set; }
    }

    public class CollectionDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string ArtUrl { get; set; }
    }

    public class GenreDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Color { get; set; }     // Hex Code
        public string IconClass { get; set; } // FontAwesome class
    }

    public class SongDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string CoverArt { get; set; }
    }

    // Add to Models/ViewModels.cs

    public class SearchViewModel : BaseSignalRViewModel
    {
        public string Query { get; set; }
        public List<User> Users { get; set; } = new();
        public List<PostDto> Posts { get; set; } = new();
    }
}
