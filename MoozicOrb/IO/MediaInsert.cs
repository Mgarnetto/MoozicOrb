using MySql.Data.MySqlClient;
using System;

namespace MoozicOrb.IO
{
    public class InsertAudio
    {
        public long Execute(int userId, string title, string filePath, string snippetPath, int duration)
        {
            string sql = @"INSERT INTO media_audio (user_id, title, file_path, snippet_path, duration_sec) 
                           VALUES (@uid, @title, @path, @snip, @dur); 
                           SELECT LAST_INSERT_ID();";

            using (var conn = new MySqlConnection(DBConn1.ConnectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@uid", userId);
                    cmd.Parameters.AddWithValue("@title", title ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@path", filePath);
                    cmd.Parameters.AddWithValue("@snip", snippetPath ?? "");
                    cmd.Parameters.AddWithValue("@dur", duration);

                    return Convert.ToInt64(cmd.ExecuteScalar());
                }
            }
        }
    }

    public class InsertVideo
    {
        public long Execute(int userId, string title, string filePath, string thumbPath, int duration, int width, int height)
        {
            string sql = @"INSERT INTO media_video (user_id, title, file_path, thumb_path, duration_sec, width, height) 
                           VALUES (@uid, @title, @path, @snip, @dur, @w, @h); 
                           SELECT LAST_INSERT_ID();";

            using (var conn = new MySqlConnection(DBConn1.ConnectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@uid", userId);
                    cmd.Parameters.AddWithValue("@title", title ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@path", filePath);
                    cmd.Parameters.AddWithValue("@snip", thumbPath ?? "");
                    cmd.Parameters.AddWithValue("@dur", duration);
                    cmd.Parameters.AddWithValue("@w", width);
                    cmd.Parameters.AddWithValue("@h", height);

                    return Convert.ToInt64(cmd.ExecuteScalar());
                }
            }
        }
    }

    public class InsertImage
    {
        public long Execute(int userId, string title, string filePath, int width, int height)
        {
            string sql = @"INSERT INTO media_images (user_id, title, file_path, width, height) 
                           VALUES (@uid, @title, @path, @w, @h); 
                           SELECT LAST_INSERT_ID();";

            using (var conn = new MySqlConnection(DBConn1.ConnectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@uid", userId);
                    cmd.Parameters.AddWithValue("@title", title ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@path", filePath);
                    cmd.Parameters.AddWithValue("@w", width);
                    cmd.Parameters.AddWithValue("@h", height);

                    return Convert.ToInt64(cmd.ExecuteScalar());
                }
            }
        }
    }
}