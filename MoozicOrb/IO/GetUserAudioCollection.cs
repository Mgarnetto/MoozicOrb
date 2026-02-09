using MoozicOrb.Models;
using MySql.Data.MySqlClient;
using System.Collections.Generic;

namespace MoozicOrb.IO
{
    public class GetUserAudioCollection
    {
        public CollectionDto Execute(int userId)
        {
            var collection = new CollectionDto
            {
                Id = -1,
                Title = "Discography",
                Description = "All audio uploads",
                ArtUrl = "/img/default_cover.jpg",
                Items = new List<CollectionItemDto>()
            };

            string sql = @"
                SELECT 
                    pm.media_id, 
                    ma.title AS song_title, 
                    ma.file_path, 
                    p.image_url AS post_art,
                    p.title AS post_title,
                    u.display_name
                FROM posts p
                JOIN post_media pm ON p.post_id = pm.post_id
                JOIN media_audio ma ON pm.media_id = ma.audio_id
                JOIN user u ON p.user_id = u.user_id
                WHERE p.user_id = @uid 
                  AND pm.media_type = 1 
                ORDER BY p.created_at DESC";

            using (var conn = new MySqlConnection(DBConn1.ConnectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@uid", userId);
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            string art = rdr["post_art"]?.ToString();
                            string rawPath = rdr["file_path"]?.ToString();

                            // --- FIX START: Ensure path has leading slash ---
                            if (!string.IsNullOrEmpty(rawPath) && !rawPath.StartsWith("/"))
                            {
                                rawPath = "/" + rawPath;
                            }
                            // --- FIX END ---

                            // If collection cover is still default, grab the first track's art
                            if (collection.ArtUrl == "/img/default_cover.jpg" && !string.IsNullOrEmpty(art))
                                collection.ArtUrl = art;

                            collection.Items.Add(new CollectionItemDto
                            {
                                TargetId = rdr.GetInt64("media_id"),
                                TargetType = 1, // Audio
                                Title = !string.IsNullOrEmpty(rdr["song_title"]?.ToString()) ? rdr["song_title"].ToString() : rdr["post_title"]?.ToString(),
                                Url = rawPath, // Use the fixed path here
                                ArtUrl = art,
                                ArtistName = rdr["display_name"]?.ToString()
                            });
                        }
                    }
                }
            }
            return collection;
        }
    }
}