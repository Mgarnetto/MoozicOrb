using MySql.Data.MySqlClient;
using System;

namespace MoozicOrb.IO
{
    public class InsertAudio
    {
        public long Execute(int userId, string title, string filePath, string snippetPath, int duration)
        {
            string sql = @"INSERT INTO media_audio (user_id, title, file_path, snippet_path, duration_sec, created_at)
                           VALUES (@uid, @title, @path, @snip, @dur, NOW()); SELECT LAST_INSERT_ID();";
            return InsertMediaBase.Run(sql, userId, title, filePath, snippetPath, duration, 0, 0);
        }
    }

    public class InsertVideo
    {
        public long Execute(int userId, string title, string filePath, string thumbPath, int duration, int width, int height)
        {
            string sql = @"INSERT INTO media_video (user_id, title, file_path, thumb_path, duration_sec, width, height, created_at)
                           VALUES (@uid, @title, @path, @snip, @dur, @w, @h, NOW()); SELECT LAST_INSERT_ID();";
            return InsertMediaBase.Run(sql, userId, title, filePath, thumbPath, duration, width, height);
        }
    }

    public class InsertImage
    {
        public long Execute(int userId, string title, string filePath, int width, int height)
        {
            string sql = @"INSERT INTO media_images (user_id, title, file_path, width, height, created_at)
                           VALUES (@uid, @title, @path, @w, @h, NOW()); SELECT LAST_INSERT_ID();";
            return InsertMediaBase.Run(sql, userId, title, filePath, null, 0, width, height);
        }
    }

    // Shared Helper to reduce copy-paste code in this snippet
    internal static class InsertMediaBase
    {
        public static long Run(string sql, int uid, string title, string path, string snip, int dur, int w, int h)
        {
            using (var conn = new MySqlConnection(DBConn1.ConnectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@uid", uid);
                    cmd.Parameters.AddWithValue("@title", title);
                    cmd.Parameters.AddWithValue("@path", path);
                    if (snip != null) cmd.Parameters.AddWithValue("@snip", snip);
                    if (dur > 0) cmd.Parameters.AddWithValue("@dur", dur);
                    if (w > 0) cmd.Parameters.AddWithValue("@w", w);
                    if (h > 0) cmd.Parameters.AddWithValue("@h", h);
                    return Convert.ToInt64(cmd.ExecuteScalar());
                }
            }
        }
    }
}