using MySql.Data.MySqlClient;
using MoozicOrb.Models; // Assuming UserSession model is here
using System;
// to be used with DbSessionStore in the future to serialize sessions in the DB
namespace MoozicOrb.IO
{
    //public class SessionIO
    //{
    //    // 1. CREATE SESSION
    //    public void Create(string sessionId, int userId)
    //    {
    //        var db = new DBConn1();
    //        using var conn = db.Connection();
    //        conn.Open();

    //        string sql = "INSERT INTO user_sessions (session_id, user_id, last_active) VALUES (@sid, @uid, NOW())";
    //        using var cmd = new MySqlCommand(sql, conn);
    //        cmd.Parameters.AddWithValue("@sid", sessionId);
    //        cmd.Parameters.AddWithValue("@uid", userId);
    //        cmd.ExecuteNonQuery();
    //    }

    //    // 2. GET SESSION (Recreates the 'Instance' on Server)
    //    public UserSession? Get(string sessionId)
    //    {
    //        var db = new DBConn1();
    //        using var conn = db.Connection();
    //        conn.Open();

    //        // Fetch only if active (you can add timeout logic here like 'AND last_active > DATE_SUB(NOW(), INTERVAL 30 DAY)')
    //        string sql = "SELECT user_id FROM user_sessions WHERE session_id = @sid AND is_active = 1";

    //        using var cmd = new MySqlCommand(sql, conn);
    //        cmd.Parameters.AddWithValue("@sid", sessionId);

    //        var result = cmd.ExecuteScalar();
    //        if (result != null && int.TryParse(result.ToString(), out int userId))
    //        {
    //            // Update Heartbeat (Keep it alive)
    //            UpdateHeartbeat(sessionId, conn);

    //            return new UserSession
    //            {
    //                SessionId = sessionId,
    //                UserId = userId
    //            };
    //        }
    //        return null;
    //    }

    //    // 3. HEARTBEAT (Updates last_active)
    //    private void UpdateHeartbeat(string sessionId, MySqlConnection conn)
    //    {
    //        string sql = "UPDATE user_sessions SET last_active = NOW() WHERE session_id = @sid";
    //        using var cmd = new MySqlCommand(sql, conn);
    //        cmd.Parameters.AddWithValue("@sid", sessionId);
    //        cmd.ExecuteNonQuery();
    //    }

        // 4. DESTROY SESSION (Logout)
        //public void Remove(string sessionId)
        //{
        //    var db = DBConn1.ConnectionString;
            

        //    // We soft delete so we have history, or use DELETE to keep table small
        //    string sql = "DELETE FROM user_sessions WHERE session_id = @sid";

        //    new NonQuery().
        //    cmd.Parameters.AddWithValue("@sid", sessionId);
        //    cmd.ExecuteNonQuery();
        //}
    //}
}
