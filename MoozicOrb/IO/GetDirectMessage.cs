using System.Data;
using MoozicOrb.Api.Models;

namespace MoozicOrb.IO
{
    public class GetDirectMessages
    {
        public GetDirectMessages() { }

        public object[] GetMessagesBetweenUsers(int userId1, int userId2)
        {
            string queryString = $@"
                SELECT * FROM direct_messages
                WHERE (sender_id = {userId1} AND receiver_id = {userId2})
                   OR (sender_id = {userId2} AND receiver_id = {userId1})
                ORDER BY timestamp ASC";

            Query query = new Query();
            DataTable dt = query.Run(queryString);

            return MapDataTable(dt);
        }

        public object[] GetMessageById(long messageId)
        {
            string queryString = $"SELECT * FROM direct_messages WHERE message_id = {messageId}";
            Query query = new Query();
            DataTable dt = query.Run(queryString);

            return MapDataTable(dt);
        }

        private object[] MapDataTable(DataTable dt)
        {
            if (dt == null || dt.Rows.Count == 0) return new object[0];

            DirectMessageDto[] messages = new DirectMessageDto[dt.Rows.Count];
            int i = 0;

            foreach (DataRow row in dt.Rows)
            {
                messages[i++] = new DirectMessageDto
                {
                    MessageId = long.Parse(row["message_id"].ToString()),
                    SenderId = int.Parse(row["sender_id"].ToString()),
                    ReceiverId = int.Parse(row["receiver_id"].ToString()),
                    Text = row["message_text"].ToString(),
                    Timestamp = (DateTime)row["timestamp"],
                    SenderName = "",             // hydrate in service/controller
                    SenderProfilePicUrl = ""     // hydrate in service/controller
                };
            }

            return messages;
        }
    }
}


