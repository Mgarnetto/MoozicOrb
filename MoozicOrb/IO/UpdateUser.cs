using MySql.Data.MySqlClient;
using System;

namespace MoozicOrb.IO
{
    public class UpdateUser
    {
        // Update Creator Page Info
        public bool UpdatePageSettings(int userId, string bio, string coverImage, string bookingEmail, string layoutJson)
        {
            string sql = @"
        UPDATE user 
        SET bio = @bio, 
            cover_image_url = @cover,
            email = @email,
            profile_layout = @layout
        WHERE user_id = @uid";

            using (var conn = new MySqlConnection(DBConn1.ConnectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@bio", bio ?? "");
                    cmd.Parameters.AddWithValue("@cover", coverImage ?? "");
                    cmd.Parameters.AddWithValue("@email", bookingEmail ?? "");
                    // Handle null/empty here:
                    cmd.Parameters.AddWithValue("@layout", string.IsNullOrEmpty(layoutJson) ? "[\"posts\",\"music\",\"store\"]" : layoutJson);
                    cmd.Parameters.AddWithValue("@uid", userId);

                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        // Update Profile Picture
        public bool UpdateProfilePic(int userId, string picUrl)
        {
            string sql = "UPDATE user SET profile_pic = @pic WHERE user_id = @uid";
            using (var conn = new MySqlConnection(DBConn1.ConnectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@pic", picUrl);
                    cmd.Parameters.AddWithValue("@uid", userId);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        // Update Display Name
        public bool UpdateDisplayName(int userId, string displayName)
        {
            string sql = "UPDATE user SET display_name = @name WHERE user_id = @uid";
            using (var conn = new MySqlConnection(DBConn1.ConnectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@name", displayName);
                    cmd.Parameters.AddWithValue("@uid", userId);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }
    }
}