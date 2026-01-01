using System;
using System.Data;
using MoozicOrb.Api.Models;

namespace MoozicOrb.IO
{
    public class GetDirectMessages
    {
        public GetDirectMessages() { }

        // ----------------------------------------------------
        // THREAD BETWEEN TWO USERS
        // ----------------------------------------------------
        public DirectMessageDto[] GetMessagesBetweenUsers(int userId1, int userId2)
        {
            string queryString = $@"
                SELECT *
                FROM messages
                WHERE
                    message_deleted = 0
                AND (
                        (sender_id = {userId1} AND receiver_id = {userId2})
                     OR (sender_id = {userId2} AND receiver_id = {userId1})
                    )
                ORDER BY timestamp ASC
            ";

            Query query = new Query();
            DataTable dt = query.Run(queryString);

            return MapDataTable(dt);
        }

        // ----------------------------------------------------
        // SINGLE MESSAGE BY ID
        // ----------------------------------------------------
        public DirectMessageDto[] GetMessageById(long messageId)
        {
            string queryString = $@"
                SELECT *
                FROM messages
                WHERE message_id = {messageId}
                LIMIT 1
            ";

            Query query = new Query();
            DataTable dt = query.Run(queryString);

            return MapDataTable(dt);
        }

        // ----------------------------------------------------
        // ALL DMS FOR USER (LOGIN / INBOX)
        // ----------------------------------------------------
        public DirectMessageDto[] GetAllMessagesForUser(int userId)
        {
            string queryString = $@"
                SELECT *
                FROM messages
                WHERE
                    message_deleted = 0
                AND (sender_id = {userId} OR receiver_id = {userId})
                ORDER BY timestamp ASC
            ";

            Query query = new Query();
            DataTable dt = query.Run(queryString);

            return MapDataTable(dt);
        }

        // ----------------------------------------------------
        // MAP
        // ----------------------------------------------------
        private DirectMessageDto[] MapDataTable(DataTable dt)
        {
            if (dt == null || dt.Rows.Count == 0)
                return Array.Empty<DirectMessageDto>();

            DirectMessageDto[] messages = new DirectMessageDto[dt.Rows.Count];
            int i = 0;

            foreach (DataRow row in dt.Rows)
            {
                messages[i++] = new DirectMessageDto
                {
                    MessageId = Convert.ToInt64(row["message_id"]),
                    SenderId = Convert.ToInt32(row["sender_id"]),
                    ReceiverId = Convert.ToInt32(row["receiver_id"]),
                    Text = row["message_text"].ToString(),
                    Timestamp = Convert.ToDateTime(row["timestamp"]),

                    // hydrated later
                    SenderName = null,
                    SenderProfilePicUrl = null
                };
            }

            return messages;
        }
    }
}

