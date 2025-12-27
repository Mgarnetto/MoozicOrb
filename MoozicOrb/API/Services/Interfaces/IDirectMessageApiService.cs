using MoozicOrb.Api.Models;
using System.Collections.Generic;

namespace MoozicOrb.Api.Services.Interfaces
{
    public interface IDirectMessageApiService
    {
        long CreateDirectMessage(int senderId, int receiverId, string text);

        IEnumerable<DirectMessageDto> GetDirectMessages(int userId1, int userId2);

        DirectMessageDto GetDirectMessage(long messageId);
    }
}



