using System.Data;
using MoozicOrb.Api.Models;

namespace MoozicOrb.IO
{
    public class GetGroupMessages
    {
        public GetGroupMessages() { }

        public object[] GetMessagesByGroupId(long groupId)
        {
            try
            {
                string queryString = $"SELECT * FROM group_messages WHERE group_id = {groupId} ORDER BY timestamp ASC";
                Query query = new Query();
                DataTable dt = query.Run(queryString);

                return MapDataTable(dt);

            }
            catch (Exception ex)
            {
                // Log the exception (you can replace this with your logging mechanism)
                Console.WriteLine($"An error occurred while fetching group messages: {ex.Message}");
                return new object[0];
            }
        }
        public object[] GetMessageById(long groupId, long messageId)
        {
            string queryString = $"SELECT * FROM group_messages WHERE group_id = {groupId} AND message_id = {messageId}";
            Query query = new Query();
            DataTable dt = query.Run(queryString);

            return MapDataTable(dt);
        }

        private object[] MapDataTable(DataTable dt)
        {
            if (dt == null || dt.Rows.Count == 0) return new object[0];

            GroupMessageDto[] messages = new GroupMessageDto[dt.Rows.Count];
            int i = 0;

            foreach (DataRow row in dt.Rows)
            {
                messages[i++] = new GroupMessageDto
                {
                    MessageId = long.Parse(row["message_id"].ToString()),
                    GroupId = long.Parse(row["group_id"].ToString()),
                    SenderId = int.Parse(row["sender_id"].ToString()),
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


