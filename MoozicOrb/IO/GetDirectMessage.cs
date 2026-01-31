using System;
using System.Data;
using MoozicOrb.Api.Models;

namespace MoozicOrb.IO
{
    public class GetDirectMessages
    {
        public GetDirectMessages() { }

        // SHARED SQL BASE (Reduces duplication)
        private const string BASE_SQL = @"
            SELECT 
                m.message_id, m.sender_id, m.receiver_id, m.message_text, m.timestamp,
                s.first_name as s_first, s.last_name as s_last, s.profile_pic as s_pic,
                r.first_name as r_first, r.last_name as r_last, r.profile_pic as r_pic
            FROM messages m
            JOIN user s ON m.sender_id = s.user_id
            JOIN user r ON m.receiver_id = r.user_id
            WHERE m.message_deleted = 0
        ";

        public DirectMessageDto[] GetMessagesBetweenUsers(int userId1, int userId2)
        {
            string sql = $@"{BASE_SQL} 
                AND (
                    (m.sender_id = {userId1} AND m.receiver_id = {userId2})
                    OR (m.sender_id = {userId2} AND m.receiver_id = {userId1})
                )
                ORDER BY m.timestamp ASC";

            return MapDataTable(new Query().Run(sql));
        }

        public DirectMessageDto[] GetMessageById(long messageId)
        {
            // Note: Removed 'message_deleted = 0' check here to allow fetching single system messages if needed
            // Re-adding the JOINs manually since base has WHERE clause
            string sql = @"
                SELECT 
                    m.message_id, m.sender_id, m.receiver_id, m.message_text, m.timestamp,
                    s.first_name as s_first, s.last_name as s_last, s.profile_pic as s_pic,
                    r.first_name as r_first, r.last_name as r_last, r.profile_pic as r_pic
                FROM messages m
                JOIN user s ON m.sender_id = s.user_id
                JOIN user r ON m.receiver_id = r.user_id
                WHERE m.message_id = " + messageId;

            return MapDataTable(new Query().Run(sql));
        }

        public DirectMessageDto[] GetAllMessagesForUser(int userId)
        {
            string sql = $@"{BASE_SQL} 
                AND (m.sender_id = {userId} OR m.receiver_id = {userId})
                ORDER BY m.timestamp ASC";

            return MapDataTable(new Query().Run(sql));
        }

        private DirectMessageDto[] MapDataTable(DataTable dt)
        {
            if (dt == null || dt.Rows.Count == 0) return Array.Empty<DirectMessageDto>();

            var messages = new DirectMessageDto[dt.Rows.Count];
            int i = 0;

            foreach (DataRow row in dt.Rows)
            {
                // Map Sender
                string sFirst = row["s_first"]?.ToString() ?? "";
                string sLast = row["s_last"]?.ToString() ?? "";

                // Map Receiver
                string rFirst = row["r_first"]?.ToString() ?? "";
                string rLast = row["r_last"]?.ToString() ?? "";

                messages[i++] = new DirectMessageDto
                {
                    MessageId = Convert.ToInt64(row["message_id"]),
                    SenderId = Convert.ToInt32(row["sender_id"]),
                    ReceiverId = Convert.ToInt32(row["receiver_id"]),
                    Text = row["message_text"].ToString(),
                    Timestamp = Convert.ToDateTime(row["timestamp"]),

                    SenderName = $"{sFirst} {sLast}".Trim(),
                    SenderProfilePicUrl = row["s_pic"]?.ToString(),

                    ReceiverName = $"{rFirst} {rLast}".Trim(),
                    ReceiverProfilePicUrl = row["r_pic"]?.ToString()
                };
            }
            return messages;
        }
    }
}