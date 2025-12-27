using MoozicOrb.Api.Models;
using MoozicOrb.Api.Services.Interfaces;
using MoozicOrb.IO;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MoozicOrb.Api.Services
{
    public class GroupMessageApiService : IGroupMessageApiService
    {
        private readonly GetGroupMessages _getGroupMessages;
        private readonly InsertGroupMessage _insertGroupMessage;

        public GroupMessageApiService()
        {
            _getGroupMessages = new GetGroupMessages();
            _insertGroupMessage = new InsertGroupMessage();
        }

        public long CreateGroupMessage(long groupId, int senderId, string text)
        {
            // Pass primitives directly — IO already expects this
            return _insertGroupMessage.Insert(groupId, senderId, text);
        }

        public IEnumerable<GroupMessageDto> GetGroupMessages(long groupId)
        {
            var messages = _getGroupMessages.GetMessagesByGroupId(groupId);

            if (messages == null || messages.Length == 0)
                return Enumerable.Empty<GroupMessageDto>();

            return messages
                .Cast<GroupMessageDto>()
                .OrderBy(m => m.Timestamp);
        }

        public GroupMessageDto GetGroupMessage(long groupId, long messageId)
        {
            var messages = _getGroupMessages.GetMessageById(groupId, messageId);

            if (messages == null || messages.Length == 0)
                return null;

            return messages[0] as GroupMessageDto;
        }
    }
}



