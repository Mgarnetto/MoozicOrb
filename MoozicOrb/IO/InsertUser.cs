using MySql.Data.MySqlClient;
using MoozicOrb.Models;
using System;

namespace MoozicOrb.IO
{
    public class InsertUser
    {
        public long Execute(User user)
        {
            // FIXED: Removed 'is_artist'
            // FIXED: Moved 'user_groups' to the end to match schema
            string sql = @"
                INSERT INTO `user` 
                (
                    first_name, middle_name, last_name,
                    username, email, display_name,
                    profile_pic, cover_image_url, bio,
                    is_creator, profile_layout, user_groups
                )
                VALUES 
                (
                    @fname, @mname, @lname,
                    @username, @email, @display,
                    @pic, @cover, @bio,
                    @creator, @layout, @groups
                );
                SELECT LAST_INSERT_ID();";

            using (var conn = new MySqlConnection(DBConn1.ConnectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    // 1. Legal Names
                    cmd.Parameters.AddWithValue("@fname", user.FirstName ?? "");
                    cmd.Parameters.AddWithValue("@mname", user.MiddleName ?? "");
                    cmd.Parameters.AddWithValue("@lname", user.LastName ?? "");

                    // 2. Identity
                    cmd.Parameters.AddWithValue("@username", user.UserName ?? "");
                    cmd.Parameters.AddWithValue("@email", user.Email ?? "");
                    cmd.Parameters.AddWithValue("@display", user.DisplayName ?? user.UserName);

                    // 3. Profile Images & Bio
                    cmd.Parameters.AddWithValue("@pic", user.ProfilePic ?? "/img/profile_default.jpg");
                    cmd.Parameters.AddWithValue("@cover", user.CoverImageUrl ?? "/img/default_cover.jpg");
                    cmd.Parameters.AddWithValue("@bio", user.Bio ?? "");

                    // 4. Flags & Settings
                    cmd.Parameters.AddWithValue("@creator", user.IsCreator ? 1 : 0);

                    // JSON Layout (Before Groups)
                    cmd.Parameters.AddWithValue("@layout", user.ProfileLayoutJson ?? (object)DBNull.Value);

                    // 5. Groups (Last Column)
                    // Default to "9" (Standard User) if empty
                    cmd.Parameters.AddWithValue("@groups", string.IsNullOrEmpty(user.UserGroups) ? "9" : user.UserGroups);

                    return Convert.ToInt64(cmd.ExecuteScalar());
                }
            }
        }
    }
}
