using System.Data;
using MoozicOrb.Api.Models;

namespace MoozicOrb.IO
{
    public class GetStreamSessions
    {
        public DataTable GetByStream(long streamId)
        {
            string query = $@"
                SELECT ss.*, u.first_name, u.last_name, u.profile_pic
                FROM stream_sessions ss
                LEFT JOIN user u ON ss.user_id = u.user_id
                WHERE ss.stream_id = {streamId}";

            Query q = new Query();
            return q.Run(query);
        }
    }
}

