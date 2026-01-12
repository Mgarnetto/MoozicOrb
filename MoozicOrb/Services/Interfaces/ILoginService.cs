namespace MoozicOrb.Services.Interfaces
{
    public interface ILoginService
    {
        // Return just userId (sync)
        int Login(string username, string password);

        // Logout by sessionId (sync)
        void Logout(string sessionId);
    }
}

