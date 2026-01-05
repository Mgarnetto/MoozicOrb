using System;
using MySql.Data.MySqlClient;

namespace MoozicOrb.IO
{
    public class CreateStream
    {
        public long Insert(int ownerUserId, string streamType)
        {
            string query = @"
                INSERT INTO streams
                    (owner_user_id, stream_type, is_live, started_at)
                VALUES
                    (@ownerId, @type, 1, @startedAt);
                SELECT LAST_INSERT_ID();";

            using (MySqlConnection conn =
                new MySqlConnection(DBConn1.ConnectionString))
            {
                conn.Open();

                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ownerId", ownerUserId);
                    cmd.Parameters.AddWithValue("@type", streamType);
                    cmd.Parameters.AddWithValue(
                        "@startedAt",
                        DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                    );

                    return Convert.ToInt64(cmd.ExecuteScalar());
                }
            }
        }
    }
}
