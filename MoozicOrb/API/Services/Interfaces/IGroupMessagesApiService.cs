using MoozicOrb.Api.Models;

namespace MoozicOrb.Api.Services.Interfaces
{
    public interface IGroupMessageApiService
    {
        long CreateGroupMessage(long groupId, int senderId, string text);

        IEnumerable<MessageDto> GetGroupMessages(
            long groupId,
            long? sinceMessageId,
            int limit
        );

        MessageDto GetGroupMessage(long groupId, long messageId);
    }
}
