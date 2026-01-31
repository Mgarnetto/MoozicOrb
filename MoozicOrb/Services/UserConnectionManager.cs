namespace MoozicOrb.Services
{
    public class UserConnectionManager
    {
        private readonly Dictionary<int, HashSet<string>> _connections = new();
        private readonly object _lock = new();

        public void AddConnection(int userId, string connectionId)
        {
            lock (_lock)
            {
                if (!_connections.ContainsKey(userId))
                    _connections[userId] = new HashSet<string>();

                _connections[userId].Add(connectionId);
            }
        }

        public void RemoveConnection(int userId, string connectionId)
        {
            lock (_lock)
            {
                if (_connections.TryGetValue(userId, out var set))
                {
                    set.Remove(connectionId);
                    if (set.Count == 0)
                        _connections.Remove(userId);
                }
            }
        }

        // Change return type from IEnumerable<string> to List<string>
        public List<string> GetConnections(int userId)
        {
            lock (_lock)
            {
                return _connections.TryGetValue(userId, out var set)
                    ? set.ToList()
                    : new List<string>(); // Return empty list instead of Enumerable.Empty
            }
        }

        public void RemoveAllConnections(int userId)
        {
            lock (_lock)
            {
                _connections.Remove(userId);
            }
        }
    }
}

