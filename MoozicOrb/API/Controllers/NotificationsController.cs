using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MoozicOrb.API.Models;
using MoozicOrb.IO;
using MoozicOrb.Services;
using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace MoozicOrb.API.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    public class NotificationsController : ControllerBase
    {
        private readonly IHttpContextAccessor _http;

        public NotificationsController(IHttpContextAccessor http)
        {
            _http = http;
        }

        private int GetUserId()
        {
            var sid = _http.HttpContext?.Request.Headers["X-Session-Id"].ToString();
            var session = SessionStore.GetSession(sid);
            if (session == null) throw new UnauthorizedAccessException();
            return session.UserId;
        }

        [HttpGet]
        public IActionResult GetNotifications()
        {
            try
            {
                int userId = GetUserId();
                var io = new NotificationIO();
                // We reuse GetUnread or create a GetRecent method in IO
                // For now, let's assume GetUnread returns the list we need
                var list = io.GetUnread(userId);
                return Ok(list);
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpPost("mark-read")]
        public IActionResult MarkAllRead()
        {
            try
            {
                int userId = GetUserId();
                string sql = "UPDATE notifications SET is_read = 1 WHERE user_id = @uid";

                // Simple inline execution since we didn't add this to IO yet
                using (var conn = new MySqlConnection(DBConn1.ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@uid", userId);
                        cmd.ExecuteNonQuery();
                    }
                }
                return Ok();
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }
    }
}
