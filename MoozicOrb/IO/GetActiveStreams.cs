using System.Data;
using MoozicOrb.Api.Models;

namespace MoozicOrb.IO
{
    public class GetActiveStreams
    {
        public DataTable GetLive()
        {
            string query = @"
                SELECT s.*, t.name AS stream_type_name
                FROM streams s
                LEFT JOIN stream_types t
                  ON s.description_id = t.type_id
                WHERE s.is_live = 1
                ORDER BY s.started_at DESC";

            Query q = new Query();
            return q.Run(query);
        }
    }
}

