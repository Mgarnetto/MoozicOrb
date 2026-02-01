using System.Collections.Generic;
using System.Text.Json;

namespace MoozicOrb.Models
{
    public class User
    {
        // --- RESTORED ORIGINAL IDENTIFIERS ---
        public int UserId { get; set; } // Was 'Id', changed back to 'UserId'

        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }

        // --- NEW FIELDS (Additives) ---
        public string Email { get; set; }
        public string UserName { get; set; }     // Kept for URL/Login legacy
        public string DisplayName { get; set; }  // "Stage Name" (Optional)

        public string ProfilePic { get; set; }   // Changed back from ProfilePicUrl to match your DB column
        public string CoverImageUrl { get; set; }
        public string Bio { get; set; }

        public bool IsCreator { get; set; }      // Toggles "Studio Mode"
        public bool IsArtist { get; set; }       // Kept for legacy compatibility
        public string UserGroups { get; set; } = "9";

        public string ProfileLayoutJson { get; set; }

        // Helper: Use DisplayName if set, otherwise fallback to First/Last (Safe Display)
        public string PublicName => !string.IsNullOrEmpty(DisplayName)
            ? DisplayName
            : $"{FirstName} {LastName}".Trim();

        public List<string> LayoutOrder
        {
            get
            {
                if (string.IsNullOrEmpty(ProfileLayoutJson))
                    return new List<string> { "posts", "music", "store" };
                try { return JsonSerializer.Deserialize<List<string>>(ProfileLayoutJson); }
                catch { return new List<string> { "posts", "music", "store" }; }
            }
        }
    }
}