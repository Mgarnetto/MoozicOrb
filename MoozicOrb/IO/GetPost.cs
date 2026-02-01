using MoozicOrb.API.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MoozicOrb.IO
{
    public class GetPost
    {
        private const string Fields = @"
            p.post_id, p.user_id, p.context_type, p.context_id,
            p.post_type, p.title, p.content_text, p.image_url, p.created_at,
            p.price, p.location_label, p.difficulty_level, p.video_url, p.media_id, p.category,
            u.display_name, u.profile_pic_url";

        private const string Joins = @"
            FROM posts p
            JOIN users u ON p.user_id = u.id";

        // ==========================================
        // 1. GET SINGLE POST
        // ==========================================
        public PostDto Execute(long postId)
        {
            PostDto post = null;
            string sql = $@"SELECT {Fields} {Joins} WHERE p.post_id = @pid";

            using (var conn = new MySqlConnection(DBConn1.ConnectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@pid", postId);
                    using (var rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read()) post = MapReaderToDto(rdr);
                    }
                }

                // Fetch Attachments if post exists
                if (post != null)
                {
                    AttachMediaToPosts(conn, new List<PostDto> { post });
                }
            }
            return post;
        }

        // ==========================================
        // 2. GET FEED (List)
        // ==========================================
        public List<PostDto> Execute(string contextType, string contextId, int page = 1, int pageSize = 20)
        {
            var results = new List<PostDto>();
            int offset = (page - 1) * pageSize;

            string sql = $@"
                SELECT {Fields} {Joins} 
                WHERE p.context_type = @ctype AND p.context_id = @cid
                ORDER BY p.created_at DESC
                LIMIT @limit OFFSET @offset";

            using (var conn = new MySqlConnection(DBConn1.ConnectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@ctype", contextType);
                    cmd.Parameters.AddWithValue("@cid", contextId);
                    cmd.Parameters.AddWithValue("@limit", pageSize);
                    cmd.Parameters.AddWithValue("@offset", offset);

                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read()) results.Add(MapReaderToDto(rdr));
                    }
                }

                // Populate Attachments for all 20 posts at once
                if (results.Count > 0)
                {
                    AttachMediaToPosts(conn, results);
                }
            }
            return results;
        }

        // ==========================================
        // HELPER: FETCH AND STITCH MEDIA
        // ==========================================
        private void AttachMediaToPosts(MySqlConnection conn, List<PostDto> posts)
        {
            // 1. Get all Post IDs
            var ids = string.Join(",", posts.Select(p => p.Id));

            // 2. Query post_media
            // Note: We JOIN to 'media' table (assuming you have one) to get the URL
            // If you don't have a central media table yet, you might need to adjust this join.
            // For now, I'll assume we can construct the URL or fetch it.

            /* Assumption: You have a 'media' table with 'file_path'. 
               If not, and you just store paths in post_media, change this.
               Based on UploadController, you likely store file paths in specific tables (audio/video/images).
               For simplicity, let's assume post_media has what we need or we construct it.
            */

            string sql = $@"
                SELECT pm.post_id, pm.media_id, pm.media_type, pm.sort_order 
                FROM post_media pm
                WHERE pm.post_id IN ({ids})
                ORDER BY pm.sort_order ASC";

            using (var cmd = new MySqlCommand(sql, conn))
            {
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        long pId = rdr.GetInt64("post_id");
                        var post = posts.FirstOrDefault(p => p.Id == pId);
                        if (post != null)
                        {
                            post.Attachments.Add(new MediaAttachmentDto
                            {
                                MediaId = rdr.GetInt64("media_id"),
                                MediaType = rdr.GetByte("media_type"),
                                // TODO: In a real system, you'd JOIN to get the real filename.
                                // For now, the frontend will likely fetch the media details by ID 
                                // OR we add a specific IO to get URLs.
                                Url = "" // Placeholder until we link the specific media tables
                            });
                        }
                    }
                }
            }
        }

        // ==========================================
        // HELPER: MAPPER
        // ==========================================
        private PostDto MapReaderToDto(MySqlDataReader rdr)
        {
            return new PostDto
            {
                Id = rdr.GetInt64("post_id"),
                AuthorId = rdr.GetInt32("user_id"),
                AuthorName = rdr["display_name"].ToString(),
                AuthorPic = rdr["profile_pic_url"] == DBNull.Value ? "/img/default.png" : rdr["profile_pic_url"].ToString(),
                ContextType = rdr["context_type"].ToString(),
                ContextId = rdr["context_id"].ToString(),
                Type = rdr["post_type"].ToString(),
                Title = rdr["title"] == DBNull.Value ? null : rdr["title"].ToString(),
                Text = rdr["content_text"] == DBNull.Value ? null : rdr["content_text"].ToString(),
                ImageUrl = rdr["image_url"] == DBNull.Value ? null : rdr["image_url"].ToString(),
                CreatedAt = rdr.GetDateTime("created_at"),
                Price = rdr["price"] == DBNull.Value ? null : (decimal?)rdr.GetDecimal("price"),
                LocationLabel = rdr["location_label"] == DBNull.Value ? null : rdr["location_label"].ToString(),
                DifficultyLevel = rdr["difficulty_level"] == DBNull.Value ? null : rdr["difficulty_level"].ToString(),
                VideoUrl = rdr["video_url"] == DBNull.Value ? null : rdr["video_url"].ToString(),
                MediaId = rdr["media_id"] == DBNull.Value ? null : (long?)rdr.GetInt64("media_id"),
                Category = rdr["category"] == DBNull.Value ? null : rdr["category"].ToString(),
                CreatedAgo = TimeAgo(rdr.GetDateTime("created_at"))
            };
        }

        private string TimeAgo(DateTime date)
        {
            var span = DateTime.UtcNow - date;
            if (span.TotalMinutes < 60) return $"{span.Minutes}m ago";
            if (span.TotalHours < 24) return $"{span.Hours}h ago";
            return $"{span.Days}d ago";
        }
    }
}