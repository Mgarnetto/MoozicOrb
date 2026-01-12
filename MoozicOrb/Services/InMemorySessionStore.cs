using MoozicOrb.Models;
using MoozicOrb.Services.Interfaces;
using System.Collections.Concurrent;

namespace MoozicOrb.Services;

public class InMemorySessionStore : ISessionStore
{
    private static readonly ConcurrentDictionary<string, UserSession> _sessions = new();

    public Task<UserSession> CreateAsync(int userId)
    {
        var session = new UserSession
        {
            SessionId = Guid.NewGuid().ToString("N"),
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _sessions[session.SessionId] = session;
        return Task.FromResult(session);
    }

    public Task<UserSession?> GetAsync(string sessionId)
    {
        _sessions.TryGetValue(sessionId, out var session);
        return Task.FromResult(session);
    }

    public Task RemoveAsync(string sessionId)
    {
        _sessions.TryRemove(sessionId, out _);
        return Task.CompletedTask;
    }
}


