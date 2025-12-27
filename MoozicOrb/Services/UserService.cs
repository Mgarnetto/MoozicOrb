using Microsoft.AspNetCore.SignalR;

namespace MoozicOrb.Services;

public class UserService : IUserService
{
    public int GetCurrentUserId(HubCallerContext context)
    {
        // TODO: get from auth / claims
        return 10;
    }

    public string GetUserGroupsCsv(int userId)
    {
        // TODO: SELECT user_groups FROM users WHERE user_id = @userId
        return "9,12,15";
    }
}

