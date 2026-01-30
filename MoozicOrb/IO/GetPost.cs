using MoozicOrb.API.Models; 
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace MoozicOrb.IO
{
    public class GetPost
    {
        public PostDto Execute(long postId)
        {
            PostDto post = null;

            using (var conn = new MySqlConnection(DBConn1.ConnectionString))
            {
                conn.Open();

                // 1. FETCH POST & MEDIA (Joined)
                // We use LEFT JOINs to find attached media (Audio, Video, or Image)
                string sql = @"
                    SELECT 
                        p.post_id, p.user_id, p.content_text, p.post_type, p.created_at,
                        pm.media_id, pm.media_type,
                        -- Resolve Title
                        CASE 
                            WHEN pm.media_type = 1 THEN ma.title
                            WHEN pm.media_type = 2 THEN mv.title
                            WHEN pm.media_type = 3 THEN mi.title
                        END as media_title,
                        -- Resolve URL
                        CASE 
                            WHEN pm.media_type = 1 THEN ma.file_path
                            WHEN pm.media_type = 2 THEN mv.file_path
                            WHEN pm.media_type = 3 THEN mi.file_path
                        END as media_url
                    FROM posts p
                    LEFT JOIN post_media pm ON p.post_id = pm.post_id
                    LEFT JOIN media_audio ma ON pm.media_id = ma.audio_id AND pm.media_type = 1
                    LEFT JOIN media_video mv ON pm.media_id = mv.video_id AND pm.media_type = 2
                    LEFT JOIN media_images mi ON pm.media_id = mi.image_id AND pm.media_type = 3
                    WHERE p.post_id = @pid
                    ORDER BY pm.sort_order ASC";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@pid", postId);
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            if (post == null)
                            {
                                post = new PostDto
                                {
                                    PostId = Convert.ToInt64(r["post_id"]),
                                    UserId = Convert.ToInt32(r["user_id"]),
                                    Content = r["content_text"].ToString(),
                                    Type = Convert.ToInt32(r["post_type"]),
                                    CreatedAt = Convert.ToDateTime(r["created_at"]),
                                    Attachments = new List<MediaAttachmentDto>()
                                };
                            }

                            // Add attachment if exists (LEFT JOIN might be null)
                            if (r["media_id"] != DBNull.Value)
                            {
                                post.Attachments.Add(new MediaAttachmentDto
                                {
                                    MediaId = Convert.ToInt64(r["media_id"]),
                                    MediaType = Convert.ToInt32(r["media_type"]),
                                    Url = r["media_url"].ToString()
                                    // Title = r["media_title"].ToString() (Add if DTO supports it)
                                });
                            }
                        }
                    }
                }
            }
            return post;
        }
    }
}