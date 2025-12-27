namespace MoozicOrb.CO
{
    public class ConnectionSession
    {
        public string UserId { get; set; }
        public string ConnectionId { get; set; }

        // runtime HashSet of groups
        public HashSet<string> Groups { get; set; } = new HashSet<string>();

        public ConnectionSession(int user_id, string groups)
        {
            UserId = user_id + "";
            var group = groups.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(g => g.Trim());

        }

    }
}
