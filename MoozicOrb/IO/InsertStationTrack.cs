using System;
using MySql.Data.MySqlClient;

namespace MoozicOrb.IO
{
    public class InsertStationTrack
    {
        public InsertStationTrack() { }

        public void Execute(int stationId, long trackId, int sortOrder)
        {
            string sql = @"
                INSERT INTO station_playlist 
                (station_id, track_id, sort_order, added_at)
                VALUES 
                (@sid, @tid, @order, NOW());";

            using (MySqlConnection connection = new MySqlConnection(DBConn1.ConnectionString))
            {
                connection.Open();
                using (MySqlCommand command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@sid", stationId);
                    command.Parameters.AddWithValue("@tid", trackId);
                    command.Parameters.AddWithValue("@order", sortOrder);

                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        // Handle duplicate entries if necessary
                        Console.WriteLine($"Error adding track to station: {ex.Message}");
                    }
                }
            }
        }
    }
}