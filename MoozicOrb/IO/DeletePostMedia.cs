using MySql.Data.MySqlClient;

namespace MoozicOrb.IO
{
    public class DeletePostMedia
    {
        public void Execute(int userId, long postId, long mediaId)
        {
            // Complex check: Ensure user owns the post that the media is attached to
            string sql = @"
                DELETE pm FROM post_media pm
                INNER JOIN posts p ON pm.post_id = p.post_id
                WHERE pm.post_id = @pid AND pm.media_id = @mid AND p.user_id = @uid";

            using (var conn = new MySqlConnection(DBConn1.ConnectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@pid", postId);
                    cmd.Parameters.AddWithValue("@mid", mediaId);
                    cmd.Parameters.AddWithValue("@uid", userId);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}