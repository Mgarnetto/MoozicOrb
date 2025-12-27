using MoozicOrb.Models;
using MoozicOrb.IO;

namespace MoozicOrb.Services;

public class GroupMessageService : IGroupMessageService
{
    public Task SaveGroupMessageAsync(GroupMessage message)
    {
        // TODO: INSERT INTO group_messages

        new InsertGroupMessage().Insert(message.GroupId, message.SenderId, message.MessageText);

        return Task.CompletedTask;
    }
}

