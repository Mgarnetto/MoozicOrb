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
                    email, 
                    username, 
                    display_name, 
                    profile_pic, 
                    cover_image_url, 
                    bio, 
                    is_creator, 
                    user_groups,
                    profile_layout
                )
                VALUES 
                (
                    @email, 
                    @username, 
                    @display, 
                    @pic, 
                    @cover, 
                    @bio, 
                    @creator, 
                    @groups,
                    @layout
                );
                SELECT LAST_INSERT_ID();";

            using (var conn = new MySqlConnection(DBConn1.ConnectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    // Mandatory Fields
                    cmd.Parameters.AddWithValue("@email", user.Email);
                    cmd.Parameters.AddWithValue("@username", user.Username);
                    cmd.Parameters.AddWithValue("@display", user.DisplayName ?? user.Username); // Fallback to username

                    // Optional Profile Data
                    cmd.Parameters.AddWithValue("@pic", user.ProfilePicUrl ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@cover", user.CoverImageUrl ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@bio", user.Bio ?? (object)DBNull.Value);

                    // Flags & Groups
                    cmd.Parameters.AddWithValue("@creator", user.IsCreator ? 1 : 0);
                    cmd.Parameters.AddWithValue("@groups", string.IsNullOrEmpty(user.UserGroups) ? "9" : user.UserGroups);

                    // Layout Engine
                    cmd.Parameters.AddWithValue("@layout", user.ProfileLayoutJson ?? (object)DBNull.Value);

                    object id = cmd.ExecuteScalar();
                    return Convert.ToInt64(id);
                }
            }
        }
    }
}
