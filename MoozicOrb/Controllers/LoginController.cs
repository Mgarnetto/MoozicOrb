using Microsoft.AspNetCore.Mvc;
using MoozicOrb.IO;
using MoozicOrb.Models;
using MoozicOrb.Services;
using MoozicOrb.Services.Interfaces;

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

        // POST /api/login
        [HttpPost]
        public IActionResult Login([FromForm] string username, [FromForm] string password)
        {
            int userId = _loginService.Login(username, password);
            if (userId <= 0)
                return Unauthorized(new { message = "Invalid credentials" });

            // Create session
            var session = SessionStore.CreateSession(userId);
            return Ok(new { sessionId = session.SessionId, userId = session.UserId });
        }

        // POST /api/logout
        [HttpPost("logout")]
        public IActionResult Logout([FromForm] string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
                return BadRequest(new { message = "Missing sessionId" });

            _loginService.Logout(sessionId);
            return Ok(new { message = "Logged out" });
        }

        // GET /api/login/bootstrap
        [HttpGet("bootstrap")]
        public IActionResult Bootstrap([FromHeader(Name = "X-Session-Id")] string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
                return Unauthorized();

            var session = SessionStore.GetSession(sessionId);
            if (session == null)
                return Unauthorized();

            // Lookup user
            var user = new UserQuery().GetUserById(session.UserId);
            if (user == null)
                return Unauthorized();

            // Groups stored as CSV on user model
            var groupIds = (user.UserGroups ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(g => long.Parse(g.Trim()))
                .ToList();

            return Ok(new
            {
                userId = user.UserId,
                groups = groupIds
            });
        }

    }
}








