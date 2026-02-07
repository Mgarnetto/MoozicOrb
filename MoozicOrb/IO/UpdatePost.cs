using MySql.Data.MySqlClient;
using MoozicOrb.API.Models;
using System;

namespace MoozicOrb.IO
{
    public class UpdatePost
    {
        public void Execute(int userId, long postId, UpdatePostDto req)
        {
            // Security check: Ensure the user owns the post inside the SQL or check before calling
            string sql = @"
                UPDATE posts 
                SET title = @title, content_text = @text, 
                    price = @price, location_label = @loc, difficulty_level = @diff
                WHERE post_id = @pid AND user_id = @uid";

            using (var conn = new MySqlConnection(DBConn1.ConnectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@pid", postId);
                    cmd.Parameters.AddWithValue("@uid", userId);
                    cmd.Parameters.AddWithValue("@title", req.Title ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@text", req.Text ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@price", req.Price ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@loc", req.LocationLabel ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@diff", req.DifficultyLevel ?? (object)DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}