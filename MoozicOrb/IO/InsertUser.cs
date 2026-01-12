using MySql.Data.MySqlClient;
using MoozicOrb.Models;
using System;

namespace MoozicOrb.IO
{
    public class InsertUser
    {
        public long Insert(User user)
        {
            string query = @"
                INSERT INTO user
                    (profile_pic, first_name, middle_name, last_name, username, user_groups, is_artist)
                VALUES
                    (@profilePic, @firstName, @middleName, @lastName, @username, @groups, @isArtist);
                SELECT LAST_INSERT_ID();";

            using var conn = new MySqlConnection(DBConn1.ConnectionString);
            conn.Open();
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@profilePic", user.ProfilePic ?? "");
            cmd.Parameters.AddWithValue("@firstName", user.FirstName ?? "");
            cmd.Parameters.AddWithValue("@middleName", user.MiddleName ?? "");
            cmd.Parameters.AddWithValue("@lastName", user.LastName ?? "");
            cmd.Parameters.AddWithValue("@username", user.UserName ?? "");
            cmd.Parameters.AddWithValue("@groups", user.UserGroups ?? "");
            cmd.Parameters.AddWithValue("@isArtist", user.IsArtist ? 1 : 0);

            object id = cmd.ExecuteScalar();
            return Convert.ToInt64(id);
        }
    }
}
