using MySql.Data.MySqlClient;
using System;

namespace MoozicOrb.IO
{
    public class InsertPostMedia
    {
        public void Execute(long postId, long mediaId, int type, int sortOrder)
        {
            string sql = @"
                INSERT INTO post_media (post_id, media_id, media_type, sort_order)
                VALUES (@pid, @mid, @type, @sort)";

            using (var conn = new MySqlConnection(DBConn1.ConnectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@pid", postId);
                    cmd.Parameters.AddWithValue("@mid", mediaId);
                    cmd.Parameters.AddWithValue("@type", type);
                    cmd.Parameters.AddWithValue("@sort", sortOrder);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
