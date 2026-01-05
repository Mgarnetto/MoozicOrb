using System.Data;
using MoozicOrb.Api.Models;

namespace MoozicOrb.IO
{
    public class GetActiveStreams
    {
        public DataTable GetLive()
        {
            string query = @"
                SELECT *
                FROM streams
                WHERE is_live = 1";

            Query q = new Query();
            return q.Run(query);
        }
    }
}
