using System;
using MySql.Data.MySqlClient;

namespace MoozicOrb.IO
{
    public class InsertStreamSession
    {
        // Returns session_id of newly inserted session
        public long Insert(long streamId, int userId)
        {
            string query = @"
                INSERT INTO stream_sessions
                    (stream_id, user_id, joined_at, last_seen)
                VALUES
                    (@streamId, @userId, @now, @now);
                SELECT LAST_INSERT_ID();";

            using (var conn = new MySqlConnection(DBConn1.ConnectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@streamId", streamId);
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));

                    return Convert.ToInt64(cmd.ExecuteScalar());
                }
            }
        }
    }
}

