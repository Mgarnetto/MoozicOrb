using MoozicOrb.Api.Models;
using MoozicOrb.Api.Services.Interfaces;
using MoozicOrb.IO;
using System;
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
            // No domain model needed here — IO already accepts primitives
            return _insertDirectMessage.Insert(senderId, receiverId, text);
        }

        public IEnumerable<DirectMessageDto> GetDirectMessages(int userId1, int userId2)
        {
            var messages = _getDirectMessages.GetMessagesBetweenUsers(userId1, userId2);

            if (messages == null || messages.Length == 0)
                return Enumerable.Empty<DirectMessageDto>();

            return messages
                .Cast<DirectMessageDto>()
                .OrderBy(m => m.Timestamp);
        }

        public DirectMessageDto GetDirectMessage(long messageId)
        {
            var messages = _getDirectMessages.GetMessageById(messageId);

            if (messages == null || messages.Length == 0)
                return null;

            return messages[0] as DirectMessageDto;
        }
    }
}



