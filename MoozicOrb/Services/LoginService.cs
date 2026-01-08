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
            _userQuery = new UserQuery();
            _authValidator = new ValidateUserAuthLocal();
        }

        public int Login(string username, string password)
        {
            // 1. Resolve user
            User user = _userQuery.GetUserByUsername(username);

            if (user == null || user.UserId <= 0)
                return 0;

            // 2. Validate password using existing IO
            bool valid = _authValidator.Validate(user.UserId, password);

            if (!valid)
                return 0;

            // 3. Success
            return user.UserId;
        }
    }
}
