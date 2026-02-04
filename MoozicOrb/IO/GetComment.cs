using MySql.Data.MySqlClient;
using MoozicOrb.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MoozicOrb.IO
{
    public class GetComments
    {
        public List<CommentDto> Execute(long postId)
        {
            var flatList = new List<CommentDto>();

            string sql = @"
                SELECT 
                    c.comment_id, c.post_id, c.parent_comment_id, c.user_id, c.content_text, c.created_at,
                    u.display_name, u.profile_pic
                FROM comments c
                JOIN `user` u ON c.user_id = u.user_id
                WHERE c.post_id = @pid
                ORDER BY c.created_at ASC";

            using (var conn = new MySqlConnection(DBConn1.ConnectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@pid", postId);
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            // --- FIX: ROBUST IMAGE HANDLING ---
                            string dbPic = rdr["profile_pic"] != DBNull.Value ? rdr["profile_pic"].ToString() : "";
                            if (string.IsNullOrEmpty(dbPic))
                            {
                                dbPic = "/img/profile_default.jpg";
                            }

                            flatList.Add(new CommentDto
                            {
                                CommentId = rdr.GetInt64("comment_id"),
                                PostId = rdr.GetInt64("post_id"),
                                ParentId = rdr["parent_comment_id"] == DBNull.Value ? null : (long?)rdr.GetInt64("parent_comment_id"),
                                UserId = rdr.GetInt32("user_id"),
                                Content = rdr["content_text"].ToString(),
                                AuthorName = rdr["display_name"].ToString(),
                                AuthorPic = dbPic, // Now guaranteed to be valid
                                CreatedAt = rdr.GetDateTime("created_at"),
                                CreatedAgo = TimeAgo(rdr.GetDateTime("created_at")),
                                Replies = new List<CommentDto>()
                            });
                        }
                    }
                }
            }

            return BuildTree(flatList);
        }

        private List<CommentDto> BuildTree(List<CommentDto> allComments)
        {
            var rootComments = new List<CommentDto>();
            var dict = allComments.ToDictionary(c => c.CommentId);

            foreach (var comment in allComments)
            {
                if (comment.ParentId.HasValue && dict.ContainsKey(comment.ParentId.Value))
                {
                    dict[comment.ParentId.Value].Replies.Add(comment);
                }
                else
                {
                    rootComments.Add(comment);
                }
            }
            return rootComments;
        }

        private string TimeAgo(DateTime date)
        {
            var span = DateTime.UtcNow - date;
            if (span.TotalMinutes < 1) return "Just now";
            if (span.TotalMinutes < 60) return $"{span.Minutes}m";
            if (span.TotalHours < 24) return $"{span.Hours}h";
            return $"{span.Days}d";
        }
    }
}