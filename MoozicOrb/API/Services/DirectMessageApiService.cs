using MoozicOrb.Api.Models;
using MoozicOrb.Api.Services.Interfaces;
using MoozicOrb.IO;

namespace MoozicOrb.Api.Services
{
    public class DirectMessageApiService : IDirectMessageApiService
    {
        public long CreateDirectMessage(int senderId, int receiverId, string text)
        {
            var message = new DirectMessage
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                MessageText = text,
                Timestamp = DateTime.UtcNow
            };

            // DB INSERT via IO
            return new InsertDirectMessage().Insert(message.SenderId, message.ReceiverId, message.MessageText);
        }

        public IEnumerable<MessageDto> GetDirectMessages(
            int userId,
            int otherUserId,
            long? sinceMessageId,
            int limit)
        {
            // DB SELECT via IO
            //return new GetDirectMessages()
            //    .Fetch(userId, otherUserId, sinceMessageId, limit);
            return null;
        }

        public MessageDto GetDirectMessage(long messageId)
        {
            // DB SELECT single
            //return new GetDirectMessages()
            //    .FetchSingle(messageId);
            return null;
        }
    }
}
