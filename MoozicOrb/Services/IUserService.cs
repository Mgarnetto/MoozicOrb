using Microsoft.AspNetCore.SignalR;

namespace MoozicOrb.Services;

public interface IUserService
{
    int GetCurrentUserId(HubCallerContext context);
    string GetUserGroupsCsv(int userId);
}
