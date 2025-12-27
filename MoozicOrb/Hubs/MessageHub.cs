using Microsoft.AspNetCore.SignalR;
using MoozicOrb.Models;
using MoozicOrb.Services;

namespace MoozicOrb.Hubs;

public class MessageHub : Hub
{
    private readonly IUserService _userService;
    private readonly IGroupMessageService _groupMessages;
    private readonly IDirectMessageService _directMessages;

    public MessageHub(
        IUserService userService,
        IGroupMessageService groupMessages,
        IDirectMessageService directMessages)
    {
        _userService = userService;
        _groupMessages = groupMessages;
        _directMessages = directMessages;
    }

    public override async Task OnConnectedAsync()
    {
        int userId = _userService.GetCurrentUserId(Context);

        // Required for IUserIdProvider
        Context.Items["UserId"] = userId;

        string groupsCsv = _userService.GetUserGroupsCsv(userId);

        foreach (var g in groupsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                $"group:{g.Trim()}"
            );
        }

        await base.OnConnectedAsync();
    }

    // -------- GROUP CHAT --------
    public async Task SendGroupMessage(long groupId, string text)
    {
        int senderId = _userService.GetCurrentUserId(Context);

        var message = new GroupMessage
        {
            GroupId = groupId,
            SenderId = senderId,
            MessageText = text,
            Timestamp = DateTime.UtcNow
        };

        await _groupMessages.SaveGroupMessageAsync(message);

        await Clients.Group($"group:{groupId}")
            .SendAsync("OnGroupMessage", new
            {
                groupId,
                senderId,
                text,
                timestamp = message.Timestamp.ToString("HH:mm:ss")
            });
    }

    // -------- DIRECT CHAT --------
    public async Task SendDirectMessage(int receiverId, string text)
    {
        int senderId = _userService.GetCurrentUserId(Context);

        var message = new DirectMessage
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            MessageText = text,
            Timestamp = DateTime.UtcNow
        };

        await _directMessages.SaveDirectMessageAsync(message);

        await Clients.Users(
            senderId.ToString(),
            receiverId.ToString()
        )
        .SendAsync("OnDirectMessage", new
        {
            senderId,
            receiverId,
            text,
            timestamp = message.Timestamp.ToString("HH:mm:ss")
        });

        int sdfs = 9; // break point
    }
}

