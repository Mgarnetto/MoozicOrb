using System;
using MySql.Data.MySqlClient;
using BCrypt.Net;

namespace MoozicOrb.IO
{
    public class ValidateUserAuthLocal
    {
        public ValidateUserAuthLocal() { }

        /// <summary>
        /// Checks if the given password matches the stored hash.
        /// Returns true if valid.
        /// </summary>
        public bool Validate(int userId, string password)
        {
            using (var connection = new MySqlConnection(DBConn1.ConnectionString))
            {
                connection.Open();

                using (var cmd = new MySqlCommand(@"
                    SELECT password_hash
                    FROM user_auth_local
                    WHERE user_id = @userId;
                ", connection))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);

                    var result = cmd.ExecuteScalar() as string;
                    if (string.IsNullOrEmpty(result))
                        return false;

                    return BCrypt.Net.BCrypt.Verify(password, result);
                }
            }
        }
    }
}
