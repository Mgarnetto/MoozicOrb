using MoozicOrb.Api.Models;

namespace MoozicOrb.Api.Services.Interfaces
{
    public interface IDirectMessageApiService
    {
        long CreateDirectMessage(int senderId, int receiverId, string text);

        IEnumerable<MessageDto> GetDirectMessages(
            int userId,
            int otherUserId,
            long? sinceMessageId,
            int limit
        );

        MessageDto GetDirectMessage(long messageId);
    }
}
