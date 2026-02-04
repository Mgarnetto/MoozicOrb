using MoozicOrb.IO;
using MoozicOrb.Models;
using MoozicOrb.Services.Interfaces;
// will be used with SessionIO.cs in the future to serialize sessions in the DB
namespace MoozicOrb.Services
{
    //public class DbSessionStore : ISessionStore
    //{
    //    private readonly SessionIO _io;

    //    public DbSessionStore()
    //    {
    //        _io = new SessionIO();
    //    }

    //    public void CreateSession(string sessionId, int userId)
    //    {
    //        _io.Create(sessionId, userId);
    //    }

    //    public UserSession GetSession(string sessionId)
    //    {
    //        if (string.IsNullOrEmpty(sessionId)) return null;
    //        return _io.Get(sessionId);
    //    }

    //    public void RemoveSession(string sessionId)
    //    {
    //        _io.Remove(sessionId);
    //    }
    //}
}