using System;
using MySql.Data.MySqlClient;

namespace MoozicOrb.IO
{
    public class InsertStreamSession
    {
        public void Insert(long streamId, int userId)
        {
            string query = @"
                INSERT INTO stream_sessions
                    (stream_id, user_id, joined_at, last_seen)
                VALUES
                    (@streamId, @userId, @now)";

            using (MySqlConnection conn =
                new MySqlConnection(DBConn1.ConnectionString))
            {
                conn.Open();

                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@streamId", streamId);
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.Parameters.AddWithValue(
                        "@now",
                        DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                    );

                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
