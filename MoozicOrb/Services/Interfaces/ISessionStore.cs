using MoozicOrb.Models;

namespace MoozicOrb.Services.Interfaces;

public interface ISessionStore
{
    Task<UserSession> CreateAsync(int userId);
    Task<UserSession?> GetAsync(string sessionId);
    Task RemoveAsync(string sessionId);
}

