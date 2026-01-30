using MoozicOrb.API.Models; // For MediaMetadata
using MySql.Data.MySqlClient;
using System;

namespace MoozicOrb.IO
{
    // GET AUDIO
    public class GetAudio
    {
        public MediaMetadata Execute(long id)
        {
            string sql = "SELECT file_path, duration_sec FROM media_audio WHERE audio_id = @id";
            using (var conn = new MySqlConnection(DBConn1.ConnectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read()) return new MediaMetadata
                        {
                            RelativePath = r["file_path"].ToString(),
                            DurationSeconds = Convert.ToInt32(r["duration_sec"])
                        };
                    }
                }
            }
            return null;
        }
    }

    // GET VIDEO
    public class GetVideo
    {
        public MediaMetadata Execute(long id)
        {
            string sql = "SELECT file_path, thumb_path, duration_sec, width, height FROM media_video WHERE video_id = @id";
            using (var conn = new MySqlConnection(DBConn1.ConnectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read()) return new MediaMetadata
                        {
                            RelativePath = r["file_path"].ToString(),
                            SnippetPath = r["thumb_path"].ToString(),
                            DurationSeconds = Convert.ToInt32(r["duration_sec"]),
                            Width = Convert.ToInt32(r["width"]),
                            Height = Convert.ToInt32(r["height"])
                        };
                    }
                }
            }
            return null;
        }
    }

    // GET IMAGE
    public class GetImage
    {
        public MediaMetadata Execute(long id)
        {
            string sql = "SELECT file_path, width, height FROM media_images WHERE image_id = @id";
            using (var conn = new MySqlConnection(DBConn1.ConnectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read()) return new MediaMetadata
                        {
                            RelativePath = r["file_path"].ToString(),
                            Width = Convert.ToInt32(r["width"]),
                            Height = Convert.ToInt32(r["height"])
                        };
                    }
                }
            }
            return null;
        }
    }
}