using MySql.Data.MySqlClient;
using System;

namespace MoozicOrb.IO
{
    public class ToggleLike
    {
        public bool Execute(int userId, long postId)
        {
            using (var conn = new MySqlConnection(DBConn1.ConnectionString))
            {
                conn.Open();

                // Check if already liked
                string checkSql = "SELECT COUNT(*) FROM post_likes WHERE post_id = @pid AND user_id = @uid";
                long count = 0;

                using (var checkCmd = new MySqlCommand(checkSql, conn))
                {
                    checkCmd.Parameters.AddWithValue("@pid", postId);
                    checkCmd.Parameters.AddWithValue("@uid", userId);
                    count = Convert.ToInt64(checkCmd.ExecuteScalar());
                }

                if (count > 0)
                {
                    // Unlike (Remove)
                    string delSql = "DELETE FROM post_likes WHERE post_id = @pid AND user_id = @uid";
                    using (var delCmd = new MySqlCommand(delSql, conn))
                    {
                        delCmd.Parameters.AddWithValue("@pid", postId);
                        delCmd.Parameters.AddWithValue("@uid", userId);
                        delCmd.ExecuteNonQuery();
                    }
                    return false; // Result: Not Liked
                }
                else
                {
                    // Like (Insert)
                    string insSql = "INSERT INTO post_likes (post_id, user_id, liked_at) VALUES (@pid, @uid, NOW())";
                    using (var insCmd = new MySqlCommand(insSql, conn))
                    {
                        insCmd.Parameters.AddWithValue("@pid", postId);
                        insCmd.Parameters.AddWithValue("@uid", userId);
                        insCmd.ExecuteNonQuery();
                    }
                    return true; // Result: Liked
                }
            }
        }
    }
}