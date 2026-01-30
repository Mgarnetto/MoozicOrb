using MySql.Data.MySqlClient;
using System;

namespace MoozicOrb.IO
{
    public class InsertCollection
    {
        public long Execute(int userId, string title, string description, int type, long coverId)
        {
            // type: 1=Album, 2=Playlist
            // coverId: Soft link to an Image ID
            string sql = @"
                INSERT INTO collections 
                (user_id, title, description, collection_type, cover_image_id, created_at)
                VALUES 
                (@uid, @title, @desc, @type, @cover, NOW());
                SELECT LAST_INSERT_ID();";

            using (var conn = new MySqlConnection(DBConn1.ConnectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@uid", userId);
                    cmd.Parameters.AddWithValue("@title", title);
                    cmd.Parameters.AddWithValue("@desc", description ?? "");
                    cmd.Parameters.AddWithValue("@type", type);
                    cmd.Parameters.AddWithValue("@cover", coverId);

                    return Convert.ToInt64(cmd.ExecuteScalar());
                }
            }
        }
    }
}