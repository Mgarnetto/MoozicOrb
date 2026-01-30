using MySql.Data.MySqlClient;
using System;

namespace MoozicOrb.IO
{
    public class InsertPost
    {
        public long Execute(int userId, string content, int type)
        {
            // type: 1=Status, 2=Article, 3=Classified
            string sql = @"
                INSERT INTO posts (user_id, content_text, post_type, created_at)
                VALUES (@uid, @content, @type, NOW());
                SELECT LAST_INSERT_ID();";

            using (var conn = new MySqlConnection(DBConn1.ConnectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@uid", userId);
                    cmd.Parameters.AddWithValue("@content", content);
                    cmd.Parameters.AddWithValue("@type", type);
                    return Convert.ToInt64(cmd.ExecuteScalar());
                }
            }
        }
    }
}