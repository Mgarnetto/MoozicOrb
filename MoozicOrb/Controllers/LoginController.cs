using Microsoft.AspNetCore.Mvc;
using MoozicOrb.Services.Interfaces;
using MoozicOrb.Services;

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

            return Ok(new
            {
                sessionId = session.SessionId,
                userId = session.UserId
            });
        }

        // POST /api/logout
        [HttpPost("logout")]
        public IActionResult Logout([FromForm] string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
                return BadRequest(new { message = "Missing sessionId" });

            SessionStore.RemoveSession(sessionId);
            return Ok(new { message = "Logged out" });
        }
    }
}
