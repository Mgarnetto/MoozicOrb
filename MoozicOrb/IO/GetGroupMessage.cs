using System;
using System.Data;
using MoozicOrb.Api.Models;

namespace MoozicOrb.IO
{
    public class GetGroupMessages
    {
        public GetGroupMessages() { }

        public GroupMessageDto[] GetMessagesByGroupId(long groupId)
        {
            try
            {
                // JOIN user table
                string queryString = $@"
                    SELECT m.*, u.first_name, u.last_name, u.profile_pic 
                    FROM group_messages m
                    JOIN user u ON m.sender_id = u.user_id
                    WHERE m.group_id = {groupId} 
                    ORDER BY m.timestamp ASC";

                Query query = new Query();
                DataTable dt = query.Run(queryString);

                return MapDataTable(dt);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return Array.Empty<GroupMessageDto>();
            }
        }

        public GroupMessageDto[] GetMessageById(long groupId, long messageId)
        {
            string queryString = $@"
                SELECT m.*, u.first_name, u.last_name, u.profile_pic 
                FROM group_messages m
                JOIN user u ON m.sender_id = u.user_id
                WHERE m.group_id = {groupId} AND m.message_id = {messageId}";

            Query query = new Query();
            DataTable dt = query.Run(queryString);

            return MapDataTable(dt);
        }

        private GroupMessageDto[] MapDataTable(DataTable dt)
        {
            if (dt == null || dt.Rows.Count == 0) return Array.Empty<GroupMessageDto>();

            GroupMessageDto[] messages = new GroupMessageDto[dt.Rows.Count];
            int i = 0;

            foreach (DataRow row in dt.Rows)
            {
                string first = row["first_name"] != DBNull.Value ? row["first_name"].ToString() : "";
                string last = row["last_name"] != DBNull.Value ? row["last_name"].ToString() : "";

                messages[i++] = new GroupMessageDto
                {
                    MessageId = long.Parse(row["message_id"].ToString()),
                    GroupId = long.Parse(row["group_id"].ToString()),
                    SenderId = int.Parse(row["sender_id"].ToString()),
                    Text = row["message_text"].ToString(),
                    Timestamp = (DateTime)row["timestamp"],

                    // ✅ NOW POPULATED
                    SenderName = $"{first} {last}".Trim(),
                    SenderProfilePicUrl = row["profile_pic"] != DBNull.Value ? row["profile_pic"].ToString() : null
                };
            }

            return messages;
        }
    }
}


