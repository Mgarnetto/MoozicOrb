using MoozicOrb.Api.Models;
using MoozicOrb.Api.Services.Interfaces;
using MoozicOrb.IO;

namespace MoozicOrb.Api.Services
{
    public class GroupMessageApiService : IGroupMessageApiService
    {
        public long CreateGroupMessage(long groupId, int senderId, string text)
        {
            var message = new GroupMessage
            {
                GroupId = groupId,
                SenderId = senderId,
                MessageText = text,
                Timestamp = DateTime.UtcNow
            };

            // DB INSERT via IO
            return new InsertGroupMessage().Insert(message.GroupId, message.SenderId, message.MessageText);
        }

        public IEnumerable<MessageDto> GetGroupMessages(
            long groupId,
            long? sinceMessageId,
            int limit)
        {
            // DB SELECT via IO
            //return new GetGroupMessages()
            //    .Fetch(groupId, sinceMessageId, limit);
            return Enumerable.Empty<MessageDto>(); 
        }

        public MessageDto GetGroupMessage(long groupId, long messageId)
        {
            // DB SELECT single
            //return new GetGroupMessages()
            //    .FetchSingle(groupId, messageId);
            return null;
        }
    }
}
