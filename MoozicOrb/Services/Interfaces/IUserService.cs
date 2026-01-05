using Microsoft.AspNetCore.SignalR;

namespace MoozicOrb.Services.Interfaces
{
    public interface IUserService
    {
        int GetCurrentUserId(HubCallerContext context);
        string GetUserGroupsCsv(int userId);
    }
}


