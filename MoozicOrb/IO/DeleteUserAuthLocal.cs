using System;
using MySql.Data.MySqlClient;

namespace MoozicOrb.IO
{
    public class DeleteUserAuthLocal
    {
        public DeleteUserAuthLocal() { }

        /// <summary>
        /// Deletes a user and cascades to user_auth_local.
        /// Returns true if a row was deleted.
        /// </summary>
        public bool Delete(int userId)
        {
            using (var connection = new MySqlConnection(DBConn1.ConnectionString))
            {
                connection.Open();

                using (var cmd = new MySqlCommand(@"
                    DELETE FROM user
                    WHERE user_id = @userId;
                ", connection))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);
                    int affectedRows = cmd.ExecuteNonQuery();

                    return affectedRows > 0;
                }
            }
        }
    }
}
