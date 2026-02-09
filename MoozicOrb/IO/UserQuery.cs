using MySql.Data.MySqlClient;
using MoozicOrb.Models;
using System;
using System.Collections.Generic;

namespace MoozicOrb.IO
{
    public class UserQuery
    {
        public User GetUserById(int userId)
        {
            return FetchUser("SELECT * FROM `user` WHERE user_id = @val", "@val", userId);
        }

        public User GetUserByEmail(string email)
        {
            return FetchUser("SELECT * FROM `user` WHERE email = @val", "@val", email);
        }

        // NEW: Search Users
        public List<User> SearchUsers(string term)
        {
            var users = new List<User>();
            string sql = @"
                SELECT * FROM `user` 
                WHERE username LIKE @term 
                   OR display_name LIKE @term 
                   OR email LIKE @term
                LIMIT 20";

            using (var conn = new MySqlConnection(DBConn1.ConnectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@term", "%" + term + "%");
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            users.Add(MapUser(rdr));
                        }
                    }
                }
            }
            return users;
        }

        private User FetchUser(string sql, string paramName, object paramValue)
        {
            using (var conn = new MySqlConnection(DBConn1.ConnectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue(paramName, paramValue);
                    using (var rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read()) return MapUser(rdr);
                    }
                }
            }
            return null;
        }

        private User MapUser(MySqlDataReader rdr)
        {
            return new User
            {
                UserId = rdr.GetInt32("user_id"),
                FirstName = rdr["first_name"].ToString(),
                MiddleName = rdr["middle_name"].ToString(),
                LastName = rdr["last_name"].ToString(),
                UserName = rdr["username"].ToString(),
                Email = rdr["email"] == DBNull.Value ? "" : rdr["email"].ToString(),
                DisplayName = HasColumn(rdr, "display_name") ? rdr["display_name"].ToString() : rdr["username"].ToString(),
                ProfilePic = rdr["profile_pic"] == DBNull.Value ? "/img/default.png" : rdr["profile_pic"].ToString(),
                CoverImageUrl = HasColumn(rdr, "cover_image_url") ? rdr["cover_image_url"].ToString() : "",
                Bio = HasColumn(rdr, "bio") ? rdr["bio"].ToString() : "",
                IsCreator = HasColumn(rdr, "is_creator") && (Convert.ToInt32(rdr["is_creator"]) == 1),
                ProfileLayoutJson = HasColumn(rdr, "profile_layout") ? rdr["profile_layout"].ToString() : "[]",
                UserGroups = rdr["user_groups"].ToString()
            };
        }

        private bool HasColumn(MySqlDataReader rdr, string columnName)
        {
            for (int i = 0; i < rdr.FieldCount; i++)
            {
                if (rdr.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }
    }
}