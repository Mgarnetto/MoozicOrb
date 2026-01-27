using System;
using MySql.Data.MySqlClient;

namespace MoozicOrb.IO
{
    public class InsertDirectMessage
    {
        public InsertDirectMessage() { }

        // Insert a new direct message and return the auto-increment ID
        public long Insert(int senderId, int receiverId, string messageText)
        {
            string queryString = @"
                INSERT INTO messages 
                    (sender_id, receiver_id, message_text, message_read, message_deleted, timestamp)
                VALUES
                    (@senderId, @receiverId, @messageText, 0, 0, @timestamp);
                SELECT LAST_INSERT_ID();";

            using (MySqlConnection connection = new MySqlConnection(DBConn1.ConnectionString))
            {
                connection.Open();

                using (MySqlCommand command = new MySqlCommand(queryString, connection))
                {
                    command.Parameters.AddWithValue("@senderId", senderId);
                    command.Parameters.AddWithValue("@receiverId", receiverId);
                    command.Parameters.AddWithValue("@messageText", messageText);
                    command.Parameters.AddWithValue("@timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                    try
                    {
                        return Convert.ToInt64(command.ExecuteScalar());
                    }
                    catch (Exception ex)
                    {
                        // Log exception if necessary
                        return 0;
                    }
                }
            }
        }
    }
}
