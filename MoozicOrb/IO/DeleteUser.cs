using MySql.Data.MySqlClient;

namespace MoozicOrb.IO
{
    public class DeleteUser
    {
        public bool Delete(int userId)
        {
            string query = "DELETE FROM user WHERE user_id=@userId;";

            using var conn = new MySqlConnection(DBConn1.ConnectionString);
            conn.Open();
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@userId", userId);

            return cmd.ExecuteNonQuery() > 0;
        }
    }
}

