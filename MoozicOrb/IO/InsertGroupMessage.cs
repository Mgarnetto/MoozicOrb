using System;
using MySql.Data.MySqlClient;

namespace MoozicOrb.IO
{
    public class InsertGroupMessage
    {
        public InsertGroupMessage() { }

        // Insert a new group message and return the auto-increment ID
        public long Insert(long groupId, int senderId, string messageText)
        {
            string queryString = @"
                USE moozicorb;
                INSERT INTO group_messages
                    (group_id, sender_id, message_text, message_deleted, timestamp)
                VALUES
                    (@groupId, @senderId, @messageText, 0, @timestamp);
                SELECT LAST_INSERT_ID();";

            using (MySqlConnection connection = new MySqlConnection(DBConn1.ConnectionString))
            {
                connection.Open();

                using (MySqlCommand command = new MySqlCommand(queryString, connection))
                {
                    command.Parameters.AddWithValue("@groupId", groupId);
                    command.Parameters.AddWithValue("@senderId", senderId);
                    command.Parameters.AddWithValue("@messageText", messageText);
                    command.Parameters.AddWithValue("@timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                    try
                    {
                        return Convert.ToInt64(command.ExecuteScalar());
                    }
                    catch (Exception ex)
                    {
                        // Log exception if needed
                        return 0;
                    }
                }
            }
        }
    }
}
