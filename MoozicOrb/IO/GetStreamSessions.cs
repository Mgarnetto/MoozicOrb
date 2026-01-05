using System.Data;
using MoozicOrb.Api.Models;

namespace MoozicOrb.IO
{
    public class GetStreamSessions
    {
        public DataTable GetByStream(long streamId)
        {
            string query = $@"
                SELECT *
                FROM stream_sessions
                WHERE stream_id = {streamId}";

            Query q = new Query();
            return q.Run(query);
        }
    }
}
