using System.Collections.Generic;
using System.Text.Json;

namespace MoozicOrb.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string Email { get; set; }        // New Login Key
        public string Username { get; set; }     // URL Slug
        public string DisplayName { get; set; }  // UI Name
        public string ProfilePicUrl { get; set; }
        public string CoverImageUrl { get; set; }

        public string UserGroups { get; set; } = "9"; // Comma-separated group IDs
        public string Bio { get; set; }
        public bool IsCreator { get; set; }

        // The Layout Engine
        public string ProfileLayoutJson { get; set; }

        public List<string> LayoutOrder
        {
            get
            {
                if (string.IsNullOrEmpty(ProfileLayoutJson))
                    return new List<string> { "posts", "music", "store" };

                try
                {
                    return JsonSerializer.Deserialize<List<string>>(ProfileLayoutJson);
                }
                catch
                {
                    return new List<string> { "posts", "music", "store" };
                }
            }
        }
    }
}
