//using System;
//using System.Collections.Generic;
//using System.Data;
//using MySql.Data.MySqlClient;
//using MoozicOrb.Api.Models;

//namespace MoozicOrb.IO
//{
//    public class GetStations
//    {
//        public GetStations() { }

//        public List<StationDto> GetPublic()
//        {
//            // JOIN users to get the owner name. 
//            // If owner_user_id is NULL, it returns 'System' (Official Station).
//            string queryString = @"
//                SELECT 
//                    s.station_id, 
//                    s.name, 
//                    s.description, 
//                    s.visibility, 
//                    s.created_at,
//                    COALESCE(u.username, 'MoozicOrb Official') as owner_name
//                FROM stations s
//                LEFT JOIN users u ON s.owner_user_id = u.user_id
//                WHERE s.visibility = 1 -- Only Public Stations
//                ORDER BY s.station_id ASC";

//            using (MySqlConnection connection = new MySqlConnection(DBConn1.ConnectionString))
//            {
//                var stations = new List<StationDto>();

//                connection.Open();
//                using (MySqlCommand command = new MySqlCommand(queryString, connection))
//                using (var reader = command.ExecuteReader())
//                {
//                    while (reader.Read())
//                    {
//                        stations.Add(new StationDto
//                        {
//                            StationId = Convert.ToInt32(reader["station_id"]),
//                            Name = reader["name"].ToString(),
//                            Description = reader["description"].ToString(),
//                            OwnerName = reader["owner_name"].ToString(),
//                            Visibility = Convert.ToInt32(reader["visibility"]),
//                            CreatedAt = Convert.ToDateTime(reader["created_at"])
//                        });
//                    }
//                }
//                return stations;
//            }
//        }
//    }
//}