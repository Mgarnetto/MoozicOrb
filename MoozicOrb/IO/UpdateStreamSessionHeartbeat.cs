using System;
using MySql.Data.MySqlClient;

namespace MoozicOrb.IO
{
    public class UpdateStreamSessionHeartbeat
    {
        public void Touch(long streamId, int userId)
        {
            string query = @"
                UPDATE stream_sessions
                SET last_seen = @now
                WHERE stream_id = @streamId
                  AND user_id = @userId";

            using (var conn = new MySqlConnection(DBConn1.ConnectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@streamId", streamId);
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}

