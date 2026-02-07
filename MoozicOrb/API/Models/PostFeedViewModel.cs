using System.Collections.Generic;
using MoozicOrb.API.Models; 

namespace MoozicOrb.API.Models
{
    public class PostFeedViewModel
    {
        // Context: "Where are we?" (e.g., User Profile 105, State Page GA)
        public int ViewerId { get; set; } = 0; // This user's ID (0 if not logged in)
        public string ContextType { get; set; }
        public string ContextId { get; set; }

        // Permissions: "Can the current user post here?"
        public bool AllowPosting { get; set; }

        // Input Mode: "What kind of form do we show?" (standard, article, classified)
        public string InputType { get; set; }

        // Data: The initial list of posts to render immediately (Server-Side Rendering)
        public List<PostDto> InitialPosts { get; set; } = new List<PostDto>();
    }
}
