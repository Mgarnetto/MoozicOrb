using System;
using MySql.Data.MySqlClient;

namespace MoozicOrb.IO
{
    public class InsertStation
    {
        public InsertStation() { }

        // Returns the new Station ID
        public int Execute(string name, string description, int ownerUserId, int visibility)
        {
            string sql = @"
                INSERT INTO stations 
                (name, description, owner_user_id, visibility, created_at)
                VALUES 
                (@name, @desc, @ownerId, @vis, NOW());
                SELECT LAST_INSERT_ID();";

            using (MySqlConnection connection = new MySqlConnection(DBConn1.ConnectionString))
            {
                connection.Open();
                using (MySqlCommand command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@name", name);
                    command.Parameters.AddWithValue("@desc", description ?? "");
                    command.Parameters.AddWithValue("@ownerId", ownerUserId);
                    command.Parameters.AddWithValue("@vis", visibility); // 1=Public, 2=Unlisted

                    return Convert.ToInt32(command.ExecuteScalar());
                }
            }
        }
    }
}