using System;
using MySql.Data.MySqlClient;

namespace MoozicOrb.IO
{
    public class EndStream
    {
        public void Close(long streamId)
        {
            string query = @"
                UPDATE streams
                SET is_live = 0,
                    ended_at = @endedAt
                WHERE stream_id = @streamId";

            using (var conn = new MySqlConnection(DBConn1.ConnectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@streamId", streamId);
                    cmd.Parameters.AddWithValue("@endedAt", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}

