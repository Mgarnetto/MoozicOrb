using MySql.Data.MySqlClient;
using MoozicOrb.API.Models;
using System;
using System.Collections.Generic;

namespace MoozicOrb.IO
{
    public class NotificationIO
    {
        public long Insert(int userId, int actorId, string type, long refId, string message)
        {
            if (userId == actorId) return 0; // Don't notify self

            using (var conn = new MySqlConnection(DBConn1.ConnectionString))
            {
                conn.Open();
                string sql = @"
                    INSERT INTO notifications (user_id, actor_id, type, reference_id, message, created_at)
                    VALUES (@uid, @aid, @type, @ref, @msg, NOW());
                    SELECT LAST_INSERT_ID();";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@uid", userId);
                    cmd.Parameters.AddWithValue("@aid", actorId);
                    cmd.Parameters.AddWithValue("@type", type);
                    cmd.Parameters.AddWithValue("@ref", refId);
                    cmd.Parameters.AddWithValue("@msg", message);
                    return Convert.ToInt64(cmd.ExecuteScalar());
                }
            }
        }

        public List<NotificationDto> GetUnread(int userId)
        {
            var list = new List<NotificationDto>();
            string sql = @"
                SELECT n.*, u.username, u.profile_pic 
                FROM notifications n
                JOIN user u ON n.actor_id = u.user_id
                WHERE n.user_id = @uid AND n.is_read = 0
                ORDER BY n.created_at DESC LIMIT 10";

            using (var conn = new MySqlConnection(DBConn1.ConnectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@uid", userId);
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            list.Add(new NotificationDto
                            {
                                Id = r.GetInt64("id"),
                                ActorId = r.GetInt32("actor_id"),
                                ActorName = r["username"].ToString(),
                                ActorPic = r["profile_pic"]?.ToString() ?? "/img/profile_default.jpg",
                                Type = r["type"].ToString(),
                                Message = r["message"].ToString(),
                                ReferenceId = r.GetInt64("reference_id"),
                                IsRead = false
                            });
                        }
                    }
                }
            }
            return list;
        }
    }
}