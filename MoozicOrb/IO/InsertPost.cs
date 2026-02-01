using MySql.Data.MySqlClient;
using MoozicOrb.API.Models; // Ensure you reference where CreatePostDto lives
using System;

namespace MoozicOrb.IO
{
    public class InsertPost
    {
        public long Execute(int userId, CreatePostDto req)
        {
            string sql = @"
                INSERT INTO posts 
                (
                    user_id, 
                    context_type, context_id, 
                    post_type, 
                    title, content_text, image_url,
                    price, location_label, 
                    difficulty_level, video_url, 
                    media_id, category,
                    created_at
                )
                VALUES 
                (
                    @uid, 
                    @ctype, @cid, 
                    @type, 
                    @title, @content, @img,
                    @price, @loc, 
                    @diff, @vid, 
                    @mid, @cat,
                    NOW()
                );
                SELECT LAST_INSERT_ID();";

            using (var conn = new MySqlConnection(DBConn1.ConnectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    // Core
                    cmd.Parameters.AddWithValue("@uid", userId);
                    cmd.Parameters.AddWithValue("@ctype", req.ContextType ?? "user");
                    cmd.Parameters.AddWithValue("@cid", req.ContextId ?? userId.ToString());
                    cmd.Parameters.AddWithValue("@type", req.Type);
                    cmd.Parameters.AddWithValue("@title", req.Title ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@content", req.Text ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@img", req.ImageUrl ?? (object)DBNull.Value);

                    // Polymorphic Extras
                    cmd.Parameters.AddWithValue("@price", req.Price ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@loc", req.LocationLabel ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@diff", req.DifficultyLevel ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@vid", req.VideoUrl ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@mid", req.MediaId ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@cat", req.Category ?? (object)DBNull.Value);

                    return Convert.ToInt64(cmd.ExecuteScalar());
                }
            }
        }
    }
}