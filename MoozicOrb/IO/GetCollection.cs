using MoozicOrb.API.Models; // Ensure DTOs are available
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace MoozicOrb.IO
{
    public class GetCollection
    {
        public CollectionDto Execute(long collectionId)
        {
            CollectionDto result = null;

            using (var conn = new MySqlConnection(DBConn1.ConnectionString))
            {
                conn.Open();

                // 1. FETCH HEADER
                string headSql = "SELECT * FROM collections WHERE collection_id = @id";
                using (var cmd = new MySqlCommand(headSql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", collectionId);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            result = new CollectionDto
                            {
                                Id = Convert.ToInt64(r["collection_id"]),
                                Title = r["title"].ToString(),
                                Description = r["description"].ToString(),
                                Type = Convert.ToInt32(r["collection_type"]),
                                CoverImageId = Convert.ToInt64(r["cover_image_id"]),
                                Items = new List<CollectionItemDto>()
                            };
                        }
                    }
                }

                if (result == null) return null;

                // 2. FETCH ITEMS (Polymorphic Join)
                // We join all 4 possible tables to resolve the data
                string itemSql = @"
                    SELECT 
                        ci.target_id, ci.target_type,
                        -- Resolve Title
                        CASE 
                            WHEN ci.target_type = 1 THEN ma.title
                            WHEN ci.target_type = 2 THEN mv.title
                            WHEN ci.target_type = 3 THEN mi.title
                            WHEN ci.target_type = 4 THEN c.title
                        END as item_title,
                        -- Resolve Path (Collection has no file path, so NULL)
                        CASE 
                            WHEN ci.target_type = 1 THEN ma.file_path
                            WHEN ci.target_type = 2 THEN mv.file_path
                            WHEN ci.target_type = 3 THEN mi.file_path
                            ELSE NULL 
                        END as item_url
                    FROM collection_items ci
                    LEFT JOIN media_audio ma ON ci.target_id = ma.audio_id AND ci.target_type = 1
                    LEFT JOIN media_video mv ON ci.target_id = mv.video_id AND ci.target_type = 2
                    LEFT JOIN media_images mi ON ci.target_id = mi.image_id AND ci.target_type = 3
                    LEFT JOIN collections c   ON ci.target_id = c.collection_id AND ci.target_type = 4
                    WHERE ci.collection_id = @cid
                    ORDER BY ci.sort_order ASC";

                using (var cmd = new MySqlCommand(itemSql, conn))
                {
                    cmd.Parameters.AddWithValue("@cid", collectionId);
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            result.Items.Add(new CollectionItemDto
                            {
                                TargetId = Convert.ToInt64(r["target_id"]),
                                TargetType = Convert.ToInt32(r["target_type"]),
                                Title = r["item_title"]?.ToString() ?? "Unknown",
                                Url = r["item_url"]?.ToString()
                            });
                        }
                    }
                }
            }
            return result;
        }
    }
}