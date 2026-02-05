using MoozicOrb.API.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MoozicOrb.IO
{
    public class GetPost
    {
        // 1. GET SINGLE POST
        public PostDto Execute(long postId, int viewerId)
        {
            PostDto post = null;
            string sql = GetBaseSql("WHERE p.post_id = @pid");

            using (var conn = new MySqlConnection(DBConn1.ConnectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@pid", postId);
                    cmd.Parameters.AddWithValue("@vid", viewerId);
                    using (var rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read()) post = MapReaderToDto(rdr);
                    }
                }
                if (post != null) AttachMediaToPosts(conn, new List<PostDto> { post });
            }
            return post;
        }

        // 2. GET FEED (With Profile Logic Fix)
        public List<PostDto> Execute(string contextType, string contextId, int viewerId, int page = 1, int pageSize = 20)
        {
            var results = new List<PostDto>();
            int offset = (page - 1) * pageSize;
            string sql;

            // FIX: If viewing a User Profile, get ALL posts by this user (user_id), 
            // ignoring "where" they posted.
            if (contextType == "user" || contextType == "page_profile")
            {
                sql = GetBaseSql("WHERE p.user_id = @cid ORDER BY p.created_at DESC LIMIT @limit OFFSET @offset");
            }
            else
            {
                // Social Feed / Global / Location: Strict context match
                sql = GetBaseSql("WHERE p.context_type = @ctype AND p.context_id = @cid ORDER BY p.created_at DESC LIMIT @limit OFFSET @offset");
            }

            using (var conn = new MySqlConnection(DBConn1.ConnectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@ctype", contextType);
                    cmd.Parameters.AddWithValue("@cid", contextId);
                    cmd.Parameters.AddWithValue("@vid", viewerId);
                    cmd.Parameters.AddWithValue("@limit", pageSize);
                    cmd.Parameters.AddWithValue("@offset", offset);

                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read()) results.Add(MapReaderToDto(rdr));
                    }
                }
                if (results.Count > 0) AttachMediaToPosts(conn, results);
            }
            return results;
        }

        // 3. GENERIC DISCOVERY (Restored for Social Feed usage)
        public List<PostDto> GetDiscoveryFeed(int viewerId, int count = 20)
        {
            // Returns random mix of ALL post types (Text, Image, Video, Audio)
            return GetRandomPosts(viewerId, count, null);
        }

        // 4. AUDIO DISCOVERY (New: For Discovery Page only)
        public List<PostDto> GetAudioDiscoveryFeed(int viewerId, int count = 20)
        {
            // Filter: Must have Audio media (type 1)
            return GetRandomPosts(viewerId, count, "AND EXISTS (SELECT 1 FROM post_media pm WHERE pm.post_id = p.post_id AND pm.media_type = 1)");
        }

        // Shared Random Logic
        private List<PostDto> GetRandomPosts(int viewerId, int count, string additionalFilter)
        {
            var results = new List<PostDto>();
            string sql = GetBaseSql($"WHERE 1=1 {additionalFilter} ORDER BY RAND() LIMIT @limit");

            using (var conn = new MySqlConnection(DBConn1.ConnectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@vid", viewerId);
                    cmd.Parameters.AddWithValue("@limit", count);
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read()) results.Add(MapReaderToDto(rdr));
                    }
                }
                if (results.Count > 0) AttachMediaToPosts(conn, results);
            }
            return results;
        }

        // 5. GENERIC SEARCH (For Social Feed)
        public List<PostDto> SearchPosts(string term, int viewerId)
        {
            string whereClause = "WHERE (p.content_text LIKE @term OR p.title LIKE @term) ORDER BY p.created_at DESC LIMIT 20";
            return ExecuteSearch(term, viewerId, whereClause);
        }

        // 6. AUDIO SEARCH (New: For Discovery Page)
        public List<PostDto> SearchAudio(string term, int viewerId)
        {
            // Search text/title BUT restrict to posts containing Audio
            string whereClause = @"
                WHERE (p.content_text LIKE @term OR p.title LIKE @term) 
                AND EXISTS (SELECT 1 FROM post_media pm WHERE pm.post_id = p.post_id AND pm.media_type = 1)
                ORDER BY p.created_at DESC LIMIT 20";

            return ExecuteSearch(term, viewerId, whereClause);
        }

        private List<PostDto> ExecuteSearch(string term, int viewerId, string whereClause)
        {
            var results = new List<PostDto>();
            string sql = GetBaseSql(whereClause);

            using (var conn = new MySqlConnection(DBConn1.ConnectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@term", "%" + term + "%");
                    cmd.Parameters.AddWithValue("@vid", viewerId);
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read()) results.Add(MapReaderToDto(rdr));
                    }
                }
                if (results.Count > 0) AttachMediaToPosts(conn, results);
            }
            return results;
        }

        // --- SQL GENERATOR ---
        private string GetBaseSql(string whereClause)
        {
            return $@"
                SELECT 
                    p.post_id, p.user_id, p.context_type, p.context_id,
                    p.post_type, p.title, p.content_text, p.image_url, p.created_at,
                    p.price, p.location_label, p.difficulty_level, p.video_url, p.media_id, p.category,
                    u.display_name, u.profile_pic,
                    
                    (SELECT COUNT(*) FROM post_likes WHERE post_id = p.post_id) AS likes_count,
                    (SELECT COUNT(*) FROM comments WHERE post_id = p.post_id) AS comments_count,
                    (SELECT COUNT(*) FROM post_likes WHERE post_id = p.post_id AND user_id = @vid) AS is_liked

                FROM posts p
                JOIN `user` u ON p.user_id = u.user_id
                {whereClause}";
        }

        private void AttachMediaToPosts(MySqlConnection conn, List<PostDto> posts)
        {
            if (posts == null || posts.Count == 0) return;
            var ids = string.Join(",", posts.Select(p => p.Id));

            string sql = $@"
                SELECT pm.post_id, pm.media_id, pm.media_type, pm.sort_order,
                    COALESCE(img.file_path, vid.file_path, aud.file_path) AS final_url
                FROM post_media pm
                LEFT JOIN media_images img ON pm.media_id = img.id AND pm.media_type = 3
                LEFT JOIN media_video vid ON pm.media_id = vid.id AND pm.media_type = 2
                LEFT JOIN media_audio aud ON pm.media_id = aud.id AND pm.media_type = 1
                WHERE pm.post_id IN ({ids}) ORDER BY pm.sort_order ASC";

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
                            string dbPath = rdr["final_url"] == DBNull.Value ? "" : rdr["final_url"].ToString();
                            if (!string.IsNullOrEmpty(dbPath) && !dbPath.StartsWith("/")) dbPath = "/" + dbPath;

                            post.Attachments.Add(new MediaAttachmentDto
                            {
                                MediaId = rdr.GetInt64("media_id"),
                                MediaType = rdr.GetInt32("media_type"),
                                Url = dbPath
                            });
                        }
                    }
                }
            }
        }

        private PostDto MapReaderToDto(MySqlDataReader rdr)
        {
            return new PostDto
            {
                Id = rdr.GetInt64("post_id"),
                AuthorId = rdr.GetInt32("user_id"),
                AuthorName = rdr["display_name"].ToString(),
                AuthorPic = rdr["profile_pic"] == DBNull.Value ? "/img/profile_default.jpg" : rdr["profile_pic"].ToString(),
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
                LikesCount = Convert.ToInt32(rdr["likes_count"]),
                CommentsCount = Convert.ToInt32(rdr["comments_count"]),
                IsLiked = Convert.ToInt32(rdr["is_liked"]) > 0,
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