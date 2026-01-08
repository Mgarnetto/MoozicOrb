namespace MoozicOrb.Services.Interfaces
{
    public interface ILoginService
    {
        /// <summary>
        /// Validates username/password.
        /// Returns userId if successful, or 0 if authentication fails.
        /// </summary>
        int Login(string username, string password);
    }
}
