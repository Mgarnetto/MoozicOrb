using MySql.Data.MySqlClient;

namespace MoozicOrb.IO
{
    public class DeletePost
    {
        public bool Execute(int userId, long postId)
        {
            // Assumes FK constraints (ON DELETE CASCADE) handle comments/likes/media links.
            // If not, you must delete from child tables first.
            string sql = "DELETE FROM posts WHERE post_id = @pid AND user_id = @uid";

            using (var conn = new MySqlConnection(DBConn1.ConnectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@pid", postId);
                    cmd.Parameters.AddWithValue("@uid", userId);
                    int rows = cmd.ExecuteNonQuery();
                    return rows > 0;
                }
            }
        }
    }
}