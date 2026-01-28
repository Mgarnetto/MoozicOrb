using System;
using MySql.Data.MySqlClient;

namespace MoozicOrb.IO
{
    public class InsertTrack
    {
        public InsertTrack() { }

        // Returns the new Track ID (Long)
        public long Execute(string title, int? artistUserId, string filePath, int duration, int uploaderId, int visibility)
        {
            string sql = @"
                INSERT INTO tracks 
                (title, artist_user_id, file_path, duration_seconds, uploaded_by_user_id, visibility, created_at, play_count)
                VALUES 
                (@title, @artistId, @path, @duration, @uploaderId, @vis, NOW(), 0);
                SELECT LAST_INSERT_ID();";

            using (MySqlConnection connection = new MySqlConnection(DBConn1.ConnectionString))
            {
                connection.Open();
                using (MySqlCommand command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@title", title);

                    // Handle nullable int for Artist
                    if (artistUserId.HasValue)
                        command.Parameters.AddWithValue("@artistId", artistUserId.Value);
                    else
                        command.Parameters.AddWithValue("@artistId", DBNull.Value);

                    command.Parameters.AddWithValue("@path", filePath);
                    command.Parameters.AddWithValue("@duration", duration);
                    command.Parameters.AddWithValue("@uploaderId", uploaderId);
                    command.Parameters.AddWithValue("@vis", visibility);

                    return Convert.ToInt64(command.ExecuteScalar());
                }
            }
        }
    }
}