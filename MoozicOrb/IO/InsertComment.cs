using MySql.Data.MySqlClient;
using MoozicOrb.API.Models;
using System;

namespace MoozicOrb.IO
{
    public class InsertComment
    {
        public long Execute(int userId, CreateCommentDto req)
        {
            string sql = @"
                INSERT INTO comments 
                (post_id, user_id, parent_comment_id, content_text, created_at) 
                VALUES 
                (@pid, @uid, @parent, @text, NOW());
                SELECT LAST_INSERT_ID();";

            using (var conn = new MySqlConnection(DBConn1.ConnectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@pid", req.PostId);
                    cmd.Parameters.AddWithValue("@uid", userId);

                    // Handle NULL ParentId correctly for MySQL
                    if (req.ParentId.HasValue)
                        cmd.Parameters.AddWithValue("@parent", req.ParentId.Value);
                    else
                        cmd.Parameters.AddWithValue("@parent", DBNull.Value);

                    cmd.Parameters.AddWithValue("@text", req.Content);

                    return Convert.ToInt64(cmd.ExecuteScalar());
                }
            }
        }
    }
}