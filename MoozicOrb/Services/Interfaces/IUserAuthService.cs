using MoozicOrb.Models;

namespace MoozicOrb.Services.Interfaces
{
    public interface IUserAuthService
    {
        bool CreateUser(User user, string password);
        bool DeleteUser(int userId);
        bool ValidatePassword(int userId, string password);
    }
}
