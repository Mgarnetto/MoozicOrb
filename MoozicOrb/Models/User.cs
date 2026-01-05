using System;
using System.Collections.Generic;
using System.Linq;

namespace MoozicOrb.Models
{
    public class User
    {
        public int UserId { get; set; }

        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }

        public string ProfilePic { get; set; }

        // Comma-delimited group IDs: "12,19,44"
        public string UserGroups { get; set; }

        public bool IsArtist { get; set; }

        public HashSet<long> GetGroupIds()
        {
            if (string.IsNullOrWhiteSpace(UserGroups))
                return new HashSet<long>();

            return UserGroups
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => long.Parse(x.Trim()))
                .ToHashSet();
        }
    }
}
