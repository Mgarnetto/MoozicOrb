using MoozicOrb.Api.Models;
using MoozicOrb.Api.Services.Interfaces;
using MoozicOrb.IO;
using System.Collections.Generic;
using System.Linq;

namespace MoozicOrb.Api.Services
{
    public class DirectMessageApiService : IDirectMessageApiService
    {
        private readonly GetDirectMessages _getDirectMessages;
        private readonly InsertDirectMessage _insertDirectMessage;

        public DirectMessageApiService()
        {
            _getDirectMessages = new GetDirectMessages();
            _insertDirectMessage = new InsertDirectMessage();
        }

        public long CreateDirectMessage(int senderId, int receiverId, string text)
        {
            return _insertDirectMessage.Insert(senderId, receiverId, text);
        }

        public IEnumerable<DirectMessageDto> GetDirectMessages(int userId1, int userId2)
        {
            var messages = _getDirectMessages.GetMessagesBetweenUsers(userId1, userId2);

            return messages?
                .OrderBy(m => m.Timestamp)
                ?? Enumerable.Empty<DirectMessageDto>();
        }

        public DirectMessageDto GetDirectMessage(long messageId)
        {
            var messages = _getDirectMessages.GetMessageById(messageId);
            return messages?.FirstOrDefault();
        }

        public IEnumerable<DirectMessageDto> GetAllMessagesForUser(int userId)
        {
            var messages = _getDirectMessages.GetAllMessagesForUser(userId);

            return messages?
                .OrderBy(m => m.Timestamp)
                ?? Enumerable.Empty<DirectMessageDto>();
        }

        public Dictionary<int, List<DirectMessageDto>> GetAllDirectMessages(int userId)
        {
            var messages = _getDirectMessages.GetAllMessagesForUser(userId);

            if (messages == null || messages.Length == 0)
                return new Dictionary<int, List<DirectMessageDto>>();

            return messages
                .GroupBy(m =>
                    m.SenderId == userId
                        ? m.ReceiverId
                        : m.SenderId
                )
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(m => m.Timestamp).ToList()
                );
        }
    }
}




