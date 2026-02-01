using MySql.Data.MySqlClient;
using MoozicOrb.Models;
using System;

namespace MoozicOrb.IO
{
    public class InsertUser
    {
        public long Execute(User user)
        {
            string sql = @"
                INSERT INTO user 
                (
                    first_name, middle_name, last_name,
                    username, email, display_name,
                    profile_pic, cover_image_url, bio,
                    is_creator, is_artist, user_groups,
                    profile_layout
                )
                VALUES 
                (
                    @fname, @mname, @lname,
                    @username, @email, @display,
                    @pic, @cover, @bio,
                    @creator, @artist, @groups,
                    @layout
                );
                SELECT LAST_INSERT_ID();";

            using (var conn = new MySqlConnection(DBConn1.ConnectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    // Legal Names (Critical for Records)
                    cmd.Parameters.AddWithValue("@fname", user.FirstName ?? "");
                    cmd.Parameters.AddWithValue("@mname", user.MiddleName ?? "");
                    cmd.Parameters.AddWithValue("@lname", user.LastName ?? "");

                    // Identity
                    cmd.Parameters.AddWithValue("@username", user.UserName ?? "");
                    cmd.Parameters.AddWithValue("@email", user.Email ?? "");
                    cmd.Parameters.AddWithValue("@display", user.DisplayName ?? user.UserName); // Default to username if no stage name

                    // Profile
                    cmd.Parameters.AddWithValue("@pic", user.ProfilePic ?? "/img/default.png");
                    cmd.Parameters.AddWithValue("@cover", user.CoverImageUrl ?? "/img/default_cover.jpg");
                    cmd.Parameters.AddWithValue("@bio", user.Bio ?? "");

                    // Flags
                    cmd.Parameters.AddWithValue("@creator", user.IsCreator ? 1 : 0);
                    cmd.Parameters.AddWithValue("@artist", user.IsArtist ? 1 : 0);
                    cmd.Parameters.AddWithValue("@groups", string.IsNullOrEmpty(user.UserGroups) ? "9" : user.UserGroups);
                    cmd.Parameters.AddWithValue("@layout", user.ProfileLayoutJson ?? (object)DBNull.Value);

                    return Convert.ToInt64(cmd.ExecuteScalar());
                }
            }
        }
    }
}
