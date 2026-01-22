using MoozicOrb.Models;
using System;
using System.Collections.Concurrent;

namespace MoozicOrb.Services
{
    public static class SessionStore
    {
        private static readonly ConcurrentDictionary<string, UserSession> _sessions
            = new ConcurrentDictionary<string, UserSession>();

        // Create a new session
        public static UserSession CreateSession(int userId)
        {
            var session = new UserSession
            {
                SessionId = Guid.NewGuid().ToString("N"),
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _sessions[session.SessionId] = session;
            return session;
        }

        // Retrieve an existing session
        public static UserSession GetSession(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
                return null;

            _sessions.TryGetValue(sessionId, out var session);
            return session;
        }

        // Remove a session (logout)
        public static void RemoveSession(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
                return;

            _sessions.TryRemove(sessionId, out _);
        }

        public static UserSession GetSessionByUserId(int userId)
        {
            return _sessions.Values.FirstOrDefault(s => s.UserId == userId);
        }

    }
}

