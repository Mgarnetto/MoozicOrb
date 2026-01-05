using System;
using System.Data;
using MoozicOrb.Models;

namespace MoozicOrb.IO
{
    public class UserAuthQuery
    {
        public UserAuthQuery() { }

        // Get auth info for a user
        public UserAuthLocal GetAuthByUserId(int userId)
        {
            string query = $"SELECT * FROM user_auth_local WHERE user_id = {userId}";
            Query q = new Query();
            DataTable dt = q.Run(query);

            if (dt == null || dt.Rows.Count == 0)
                return null;

            var row = dt.Rows[0];

            return new UserAuthLocal
            {
                UserId = int.Parse(row["user_id"].ToString()),
                PasswordHash = row["password_hash"].ToString(),
                Salt = row["salt"].ToString(),
                CreatedAt = DateTime.Parse(row["created_at"].ToString())
            };
        }

        // Insert new auth record
        public long InsertAuth(int userId, string passwordHash, string salt)
        {
            string query = $@"
                INSERT INTO user_auth_local (user_id, password_hash, salt, created_at)
                VALUES ({userId}, '{passwordHash}', '{salt}', NOW());
                SELECT LAST_INSERT_ID();";

            Query q = new Query();
            DataTable dt = q.Run(query);

            if (dt == null || dt.Rows.Count == 0)
                return 0;

            return long.Parse(dt.Rows[0][0].ToString());
        }

        // Update password hash
        public bool UpdatePassword(int userId, string passwordHash, string salt)
        {
            string query = $@"
                UPDATE user_auth_local
                SET password_hash = '{passwordHash}', salt = '{salt}'
                WHERE user_id = {userId};";

            Query q = new Query();
            DataTable dt = q.Run(query);

            // If Run returns null, failed
            return dt != null;
        }
    }
}
