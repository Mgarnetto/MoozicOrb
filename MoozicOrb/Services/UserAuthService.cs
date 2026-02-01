using MoozicOrb.IO;
using MoozicOrb.Models;
using MoozicOrb.Services.Interfaces;
using BCrypt.Net;

namespace MoozicOrb.Services
{
    public class UserAuthService : IUserAuthService
    {
        private readonly InsertUser _insertUser;
        private readonly DeleteUser _deleteUser;
        private readonly InsertUserAuthLocal _insertAuth;

        public UserAuthService()
        {
            _insertUser = new InsertUser();
            _deleteUser = new DeleteUser();
            _insertAuth = new InsertUserAuthLocal();
        }

        public bool CreateUser(User user, string password)
        {
            // Insert user row
            long userId = _insertUser.Execute(user);

            if (userId == 0)
                return false;

            // Hash password
            string hash = BCrypt.Net.BCrypt.HashPassword(password);

            // Insert auth row
            long authId = _insertAuth.Insert(userId, hash);

            return authId > 0;
        }

        public bool DeleteUser(int userId)
        {
            // This will delete user and cascade delete auth
            return _deleteUser.Delete(userId);
        }

        public bool ValidatePassword(int userId, string password)
        {
            string storedHash = _insertAuth.GetPasswordHash(userId);

            if (string.IsNullOrEmpty(storedHash))
                return false;

            return BCrypt.Net.BCrypt.Verify(password, storedHash);
        }
    }
}
