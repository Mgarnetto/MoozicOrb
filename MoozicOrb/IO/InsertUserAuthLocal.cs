using MySql.Data.MySqlClient;
using System;

namespace MoozicOrb.IO
{
    public class InsertUserAuthLocal
    {
        public long Insert(long userId, string passwordHash)
        {
            string query = @"
                INSERT INTO user_auth_local
                    (user_id, password_hash, password_set_at)
                VALUES
                    (@userId, @passwordHash, @now);";

            using var conn = new MySqlConnection(DBConn1.ConnectionString);
            conn.Open();
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@userId", userId);
            cmd.Parameters.AddWithValue("@passwordHash", passwordHash);
            cmd.Parameters.AddWithValue("@now", DateTime.UtcNow);

            return cmd.ExecuteNonQuery() > 0 ? userId : 0;
        }

        public string GetPasswordHash(int userId)
        {
            string query = "SELECT password_hash FROM user_auth_local WHERE user_id=@userId LIMIT 1";

            using var conn = new MySqlConnection(DBConn1.ConnectionString);
            conn.Open();
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@userId", userId);

            object result = cmd.ExecuteScalar();
            return result?.ToString();
        }
    }
}
