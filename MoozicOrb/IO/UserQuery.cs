using MoozicOrb.Api.Models;
using MoozicOrb.Models;
using System.Data;

namespace MoozicOrb.IO
{
    public class UserQuery
    {
        public UserQuery() { }

        public User[] GetUsers()
        {
            string query = "SELECT * FROM `user`";
            Query q = new Query();
            DataTable dt = q.Run(query);

            User[] users = new User[dt.Rows.Count];
            int it = 0;

            foreach (DataRow dr in dt.Rows)
            {
                users[it] = new User();
                users[it].UserId = int.Parse(dr["user_id"].ToString());
                users[it].FirstName = dr["first_name"].ToString();
                //users[it].MiddleName = dr["middle_name"].ToString();
                users[it].LastName = dr["last_name"].ToString();
                it++;
            }

            return users;
        }
        public User GetUserById(int userId)
        {
            string query = $"SELECT * FROM user WHERE user_id = {userId}";

            Query q = new Query();
            DataTable dt = q.Run(query);

            if (dt == null || dt.Rows.Count == 0)
                return null;

            var row = dt.Rows[0];

            return new User
            {
                UserId = int.Parse(row["user_id"].ToString()),
                FirstName = row["first_name"].ToString(),
                MiddleName = row["middle_name"].ToString(),
                LastName = row["last_name"].ToString(),
                ProfilePic = row["profile_pic"].ToString(),
                UserGroups = row["user_groups"].ToString(),
                IsArtist = row["is_artist"].ToString() == "1"
            };
        }

        public User GetUserByUsername(string username)
        {
            string query = $"SELECT * FROM user WHERE username = '{username}'";

            Query q = new Query();
            DataTable dt = q.Run(query);

            if (dt == null || dt.Rows.Count == 0)
                return null;

            var row = dt.Rows[0];

            return new User
            {
                UserId = int.Parse(row["user_id"].ToString()),
                FirstName = row["first_name"].ToString(),
                MiddleName = row["middle_name"].ToString(),
                LastName = row["last_name"].ToString(),
                UserName = row["username"].ToString(), // email as username
                ProfilePic = row["profile_pic"].ToString(),
                UserGroups = row["user_groups"].ToString(),
                IsArtist = row["is_artist"].ToString() == "1"
            };
        }
    }
}
