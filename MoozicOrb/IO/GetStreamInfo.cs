//using System;
//using MySql.Data.MySqlClient;
//using MoozicOrb.Api.Models;

//namespace MoozicOrb.IO
//{
//    public class GetStreamInfo
//    {
//        public GetStreamInfo() { }

//        // Parameter is now long
//        public StreamInfoDto Get(long trackId)
//        {
//            string queryString = @"
//                SELECT 
//                    t.file_path, 
//                    t.uploaded_by_user_id, 
//                    t.visibility,
//                    u.is_banned
//                FROM tracks t
//                JOIN users u ON t.uploaded_by_user_id = u.user_id
//                WHERE t.track_id = @id 
//                LIMIT 1";

//            using (MySqlConnection connection = new MySqlConnection(DBConn1.ConnectionString))
//            {
//                connection.Open();
//                using (MySqlCommand command = new MySqlCommand(queryString, connection))
//                {
//                    command.Parameters.AddWithValue("@id", trackId);

//                    using (var reader = command.ExecuteReader())
//                    {
//                        if (reader.Read())
//                        {
//                            return new StreamInfoDto
//                            {
//                                TrackId = trackId,
//                                PhysicalPath = reader["file_path"].ToString(),
//                                UploadedByUserId = Convert.ToInt32(reader["uploaded_by_user_id"]),
//                                Visibility = Convert.ToInt32(reader["visibility"]),
//                                IsUploaderBanned = reader["is_banned"] != DBNull.Value && Convert.ToBoolean(reader["is_banned"])
//                            };
//                        }
//                    }
//                }
//            }
//            return null;
//        }
//    }
//}