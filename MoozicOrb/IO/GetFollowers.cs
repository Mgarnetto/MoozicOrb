using MySql.Data.MySqlClient;
using System.Collections.Generic;

namespace MoozicOrb.IO
{
    public class GetFollowers
    {
        public List<int> Execute(int userId)
        {
            var followers = new List<int>();
            // Assuming a 'user_follows' table exists: follower_id, target_user_id
            string sql = "SELECT follower_id FROM user_follows WHERE target_user_id = @uid";

            using (var conn = new MySqlConnection(DBConn1.ConnectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@uid", userId);
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read()) followers.Add(r.GetInt32(0));
                    }
                }
            }
            return followers;
        }
    }
}