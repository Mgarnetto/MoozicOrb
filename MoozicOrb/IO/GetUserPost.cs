using MoozicOrb.API.Models;
using System.Data;
using System.Text.RegularExpressions;

namespace MoozicOrb.IO
{
    public class GetUserPost
    {
        public GetUserPost()
        {

        }

        public object[] GetUserPostById(int PostId)
        {
            string queryString = $"SELECT * FROM users WHERE post_id = {PostId}";
            Query query = new Query();
            DataTable dt = query.Run(queryString);

            return MapDataTable(dt);
        }

        public object[] GetUserPosts(int UserId)
            {
            
            try
                {
                    string queryString = $"SELECT * FROM users WHERE user_id = {UserId} ORDER BY timestamp ASC";
                    Query query = new Query();
                    DataTable dt = query.Run(queryString);

                    return MapDataTable(dt);

                }
                catch (Exception ex)
                {
                    // Log the exception (you can replace this with your logging mechanism)
                    Console.WriteLine($"An error occurred while fetching group messages: {ex.Message}");
                    return new object[0];
                }
            
        }

        public object[] MapDataTable(DataTable dt)
        {
            if (dt == null || dt.Rows.Count == 0) return null;

            UserPost[] posts = new UserPost[dt.Rows.Count];

            int i = 0;

            foreach (DataRow row in dt.Rows)
            {
                UserPost post = new UserPost();
                post.PostId = Convert.ToInt64(row["post_id"]);
                post.UserId = Convert.ToInt32(row["user_id"]);
                post.MediaContentId = Convert.ToInt64(row["media_content_id"]);
                post.Content = Convert.ToString(row["content"]);
                post.Timestamp = Convert.ToDateTime(row["timestamp"]);
                posts[i++] = post;
            }

            return posts;
        }

    }
}
