using MoozicOrb.Models;
using MoozicOrb.IO;

namespace MoozicOrb.Services;

public class DirectMessageService : IDirectMessageService
{
    public Task SaveDirectMessageAsync(DirectMessage message)
    {
        // TODO: INSERT INTO direct_messages
        new InsertDirectMessage().Insert(message.SenderId, message.ReceiverId, message.MessageText);
        return Task.CompletedTask;
    }
}
