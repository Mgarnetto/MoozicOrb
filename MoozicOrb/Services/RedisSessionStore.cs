using StackExchange.Redis;
using MoozicOrb.Models;
using MoozicOrb.Services.Interfaces;
using System.Text.Json;

namespace MoozicOrb.Services
{
    public class RedisSessionStore /*: ISessionStore*/
    {
        private readonly IDatabase _db;
        private static readonly TimeSpan TTL = TimeSpan.FromHours(12);

        public RedisSessionStore(IConnectionMultiplexer redis)
        {
            _db = redis.GetDatabase();
        }

        private static string Key(string sessionId)
            => $"session:{sessionId}";

        public async Task<UserSession> CreateAsync(int userId)
        {
            var session = new UserSession
            {
                SessionId = Guid.NewGuid().ToString("N"),
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(session);

            await _db.StringSetAsync(
                Key(session.SessionId),
                json,
                TTL
            );

            return session;
        }

        public async Task<UserSession?> GetAsync(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
                return null;

            var value = await _db.StringGetAsync(Key(sessionId));
            if (!value.HasValue)
                return null;

            return JsonSerializer.Deserialize<UserSession>(value!);
        }

        public async Task RemoveAsync(string sessionId)
        {
            if (!string.IsNullOrEmpty(sessionId))
                await _db.KeyDeleteAsync(Key(sessionId));
        }
    }
}


