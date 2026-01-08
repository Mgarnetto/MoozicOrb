using System;
using MySql.Data.MySqlClient;

namespace MoozicOrb.IO
{
    public class CreateStream
    {
        public long Insert(
            int? ownerUserId,
            string generatedBy,      // "user" or "system"
            string mediaType,        // "audio", "video", "both"
            string title,
            int descriptionId        // FK to stream_types table
        )
        {
            string query = @"
                INSERT INTO streams
                    (owner_user_id, generated_by, media_type, title, description_id, is_live, started_at)
                VALUES
                    (@ownerId, @generatedBy, @mediaType, @title, @descriptionId, 1, @startedAt);
                SELECT LAST_INSERT_ID();";

            using (var conn = new MySqlConnection(DBConn1.ConnectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ownerId", ownerUserId.HasValue ? ownerUserId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@generatedBy", generatedBy);
                    cmd.Parameters.AddWithValue("@mediaType", mediaType);
                    cmd.Parameters.AddWithValue("@title", title);
                    cmd.Parameters.AddWithValue("@descriptionId", descriptionId);
                    cmd.Parameters.AddWithValue("@startedAt", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));

                    return Convert.ToInt64(cmd.ExecuteScalar());
                }
            }
        }
    }
}

