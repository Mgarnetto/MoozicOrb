using MySql.Data.MySqlClient;
using MoozicOrb.Models;
using System;

namespace MoozicOrb.IO
{
    public class UserQuery
    {
        // ==========================================
        // 1. GET USER BY ID (For Profile Pages)
        // ==========================================
        public User GetUserById(int userId)
        {
            User user = null;
            string sql = "SELECT * FROM user WHERE user_id = @uid";

            using (var conn = new MySqlConnection(DBConn1.ConnectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@uid", userId);
                    using (var rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read()) user = MapReaderToUser(rdr);
                    }
                }
            }
            return user;
        }

        // ==========================================
        // 2. GET USER BY EMAIL (For Login)
        // ==========================================
        public User GetUserByEmail(string email)
        {
            User user = null;
            string sql = "SELECT * FROM user WHERE email = @email";

            using (var conn = new MySqlConnection(DBConn1.ConnectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@email", email);
                    using (var rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read()) user = MapReaderToUser(rdr);
                    }
                }
            }
            return user;
        }

        // ==========================================
        // HELPER: MAPPER
        // ==========================================
        private User MapReaderToUser(MySqlDataReader rdr)
        {
            return new User
            {
                UserId = rdr.GetInt32("user_id"),
                Email = rdr["email"].ToString(),
                Username = rdr["username"].ToString(),
                DisplayName = rdr["display_name"].ToString(),

                // Handle optional profile images
                ProfilePicUrl = rdr["profile_pic"] == DBNull.Value
                    ? "/img/default.png"
                    : rdr["profile_pic"].ToString(),

                CoverImageUrl = rdr["cover_image_url"] == DBNull.Value
                    ? "/img/default_cover.jpg"
                    : rdr["cover_image_url"].ToString(),

                Bio = rdr["bio"] == DBNull.Value
                    ? ""
                    : rdr["bio"].ToString(),

                // Map boolean flag
                IsCreator = rdr["is_creator"] != DBNull.Value && (Convert.ToInt32(rdr["is_creator"]) == 1),

                // Map JSON Layout (Stored as string here, Model parses it)
                ProfileLayoutJson = rdr["profile_layout"] == DBNull.Value
                    ? null
                    : rdr["profile_layout"].ToString(),

                // Map the User Groups (Defaults to "9" if null/empty in DB)
                UserGroups = rdr["user_groups"] == DBNull.Value
                    ? "9"
                    : rdr["user_groups"].ToString()
            };
        }
    }
}