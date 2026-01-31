using Microsoft.AspNetCore.Mvc;
using MoozicOrb.IO;
using MoozicOrb.Models;
using MoozicOrb.Services;
using MoozicOrb.Services.Interfaces;
using System;
using System.Linq;

namespace MoozicOrb.Controllers
{
    [ApiController]
    [Route("api/login")]
    public class LoginController : ControllerBase
    {
        private readonly ILoginService _loginService;

        public LoginController(ILoginService loginService)
        {
            _loginService = loginService;
        }

        [HttpPost]
        public IActionResult Login([FromForm] string username, [FromForm] string password)
        {
            int userId = _loginService.Login(username, password);
            if (userId <= 0)
                return Unauthorized(new { message = "Invalid credentials" });

            var session = SessionStore.CreateSession(userId);
            return Ok(new { sessionId = session.SessionId, userId = session.UserId });
        }

        [HttpPost("logout")]
        public IActionResult Logout([FromForm] string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
                return BadRequest(new { message = "Missing sessionId" });

            _loginService.Logout(sessionId);
            return Ok(new { message = "Logged out" });
        }

        // KEEP THIS: It is lighter now (Groups only)
        [HttpGet("bootstrap")]
        public IActionResult Bootstrap([FromHeader(Name = "X-Session-Id")] string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId)) return Unauthorized();

            var session = SessionStore.GetSession(sessionId);
            if (session == null) return Unauthorized();

            // 1. Get User
            var user = new UserQuery().GetUserById(session.UserId);
            if (user == null) return Unauthorized();

            // 2. Get Groups (Critical for SignalR)
            var groupIds = (user.UserGroups ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(g => long.Parse(g.Trim()))
                .ToList();

            // Return minimal data
            return Ok(new
            {
                userId = user.UserId,
                groups = groupIds
            });
        }
    }
}








