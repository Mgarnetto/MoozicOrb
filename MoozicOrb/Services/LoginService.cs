using MoozicOrb.IO;
using MoozicOrb.Models;
using MoozicOrb.Services.Interfaces;

namespace MoozicOrb.Services
{
    public class LoginService : ILoginService
    {
        private readonly UserQuery _userQuery;
        private readonly ValidateUserAuthLocal _authValidator;

        public LoginService()
        {
            _userQuery = new UserQuery();                 // Real user lookup
            _authValidator = new ValidateUserAuthLocal(); // Real password validator
        }

        // Attempt login using username/password
        // Returns userId if success, 0 if invalid
        public int Login(string username, string password)
        {
            // 1. Lookup user
            User user = _userQuery.GetUserByEmail(username);
            if (user == null || user.UserId <= 0)
                return 0;

            // 2. Validate password
            bool valid = _authValidator.Validate(user.UserId, password);
            if (!valid)
                return 0;

            // 3. Success
            return user.UserId;
        }

        // Logout by removing session
        public void Logout(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
                return;

            SessionStore.RemoveSession(sessionId);
        }
    }
}



