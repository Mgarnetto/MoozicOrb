using Microsoft.AspNetCore.SignalR;
using MoozicOrb.Services.Interfaces;
using System.Collections.Generic;

namespace MoozicOrb.Services
{
    public class UserService : IUserService
    {
        // TEMP: Dummy users until login/auth is implemented
        private static readonly Dictionary<int, string> DummyGroups = new()
        {
            { 1, "1,2,3" },
            { 2, "2,3" },
            { 3, "1,3" },
            { 9, "9" } // Your test group
        };

        public int GetCurrentUserId(HubCallerContext context)
        {
            // For now, return a dummy user ID
            // Later, extract from context.Items or auth token
            return 1;
        }

        public string GetUserGroupsCsv(int userId)
        {
            if (DummyGroups.TryGetValue(userId, out string groups))
                return groups;

            return ""; // no groups
        }
    }
}



