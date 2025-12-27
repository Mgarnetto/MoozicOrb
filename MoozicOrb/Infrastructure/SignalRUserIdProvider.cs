using Microsoft.AspNetCore.SignalR;

namespace MoozicOrb.Infrastructure;

public class SignalRUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        return connection.Items.TryGetValue("UserId", out var id)
            ? id?.ToString()
            : null;
    }
}
