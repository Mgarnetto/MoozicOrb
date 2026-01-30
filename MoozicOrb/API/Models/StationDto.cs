using System;

namespace MoozicOrb.API.Models
{
    public class StationDto
    {
        public int StationId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string OwnerName { get; set; } // "Official Radio" or "User123"
        public int Visibility { get; set; }   // 1 = Public, 2 = Unlisted
        public DateTime CreatedAt { get; set; }

        // Future-proofing:
        public int ListenerCount { get; set; }
    }
}