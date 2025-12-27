using System;
using MySql.Data.MySqlClient;

namespace MoozicOrb.IO { 
    public class InsertDirectMessage
    {
        public InsertDirectMessage()
        {
            // Default constructor
        }

        public long Insert(int sender_id, int receiver_id, string message_text)
        {


            string queryString = "USE moozicorb; INSERT INTO messages (sender_id, receiver_id, message_text, message_read, message_deleted, timestamp) " +
                                   "VALUES (" + sender_id + ", " + receiver_id + ", '" + message_text + "', " + 0 + ", " + 0 + ", " +
                                   " '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "' );" +
                                   "SELECT LAST_INSERT_ID();";

            using (MySqlConnection connection = new MySqlConnection(DBConn1.ConnectionString))
            {
                connection.Open();

                using (MySqlCommand command = new MySqlCommand(queryString, connection))
                {
                    //command.Parameters.AddWithValue("@senderID", message.senderID);
                    //command.Parameters.AddWithValue("@receiverID", message.receiverID);
                    //command.Parameters.AddWithValue("@readMessage", message.readMessage);
                    //command.Parameters.AddWithValue("@sent", message.sent);
                    //command.Parameters.AddWithValue("@deleted", message.deleted);
                    //command.Parameters.AddWithValue("@messageText", message.messageText);
                    //command.Parameters.AddWithValue("@DateTime", message.DateTime);

                    // Execute the query and return the auto-incremented ID
                    try
                    {
                        long a = Convert.ToInt64(command.ExecuteScalar());
                        return a;
                    }
                    catch (Exception ex)
                    {
                        string msg = ex.Message.ToString();
                        int a = 0;
                    }

                    return 0;
                }
            }
        }
    }
}