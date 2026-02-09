using Microsoft.AspNetCore.Mvc;
using MoozicOrb.Extensions;
using MoozicOrb.Models;
using MoozicOrb.IO;
using MoozicOrb.Services;
using MoozicOrb.Services.Interfaces;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace MoozicOrb.Controllers
{
    [Route("settings")]
    public class SettingsController : Controller
    {
        private readonly UserQuery _userQuery;
        private readonly IUserAuthService _authService; // For password changes

        public SettingsController(IUserAuthService authService)
        {
            _userQuery = new UserQuery();
            _authService = authService; // You'll need to inject this
        }

        private int GetUserId()
        {
            string sid = Request.Headers["X-Session-Id"].ToString();
            var session = SessionStore.GetSession(sid);
            return session?.UserId ?? HttpContext.Session.GetInt32("UserId") ?? 0;
        }

        // ------------------------------------
        // PAGE SETTINGS (Bio, Cover)
        // ------------------------------------
        [HttpGet("page")]
        public IActionResult Page()
        {
            int userId = GetUserId();
            if (userId == 0) return RedirectToAction("Index", "Home");

            var user = _userQuery.GetUserById(userId);
            if (user == null) return NotFound();

            var model = new PageSettingsViewModel
            {
                Bio = user.Bio,
                CoverImage = user.CoverImageUrl,
                BookingEmail = user.Email,
                LayoutOrder = user.LayoutOrder
            };

            if (Request.IsSpaRequest()) return PartialView("_PageSettingsPartial", model);
            return RedirectToAction("Index", "Home");
        }

        [HttpPost("update-page")]
        public IActionResult UpdatePage([FromBody] PageSettingsViewModel model)
        {
            int userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var updateIo = new UpdateUser();

            string json = model.LayoutOrder != null ?
                          System.Text.Json.JsonSerializer.Serialize(model.LayoutOrder) :
                          "[]";

            bool success = updateIo.UpdatePageSettings(userId, model.Bio, model.CoverImage, model.BookingEmail, json);

            if (success) return Ok(new { success = true });
            return BadRequest("Failed.");
        }

        // ------------------------------------
        // ACCOUNT SETTINGS (Avatar, Password)
        // ------------------------------------
        [HttpGet("account")]
        public IActionResult Account()
        {
            int userId = GetUserId();
            if (userId == 0) return RedirectToAction("Index", "Home");

            var user = _userQuery.GetUserById(userId);

            var model = new AccountSettingsViewModel
            {
                DisplayName = user.DisplayName,
                Email = user.Email,
                ProfilePic = user.ProfilePic
            };

            if (Request.IsSpaRequest()) return PartialView("_AccountSettingsPartial", model);
            //return View("Account", model);
            return RedirectToAction("Index", "Home"); 
        }

        [HttpPost("update-account")]
        public IActionResult UpdateAccount([FromBody] AccountSettingsViewModel model)
        {
            int userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var updateIo = new UpdateUser();
            // Update Display Name
            bool nameSuccess = updateIo.UpdateDisplayName(userId, model.DisplayName);

            return Ok(new { success = nameSuccess });
        }

        [HttpPost("update-avatar")]
        public IActionResult UpdateAvatar([FromBody] dynamic req)
        {
            int userId = GetUserId();
            if (userId == 0) return Unauthorized();

            string url = req.GetProperty("url").ToString();
            var updateIo = new UpdateUser();
            bool success = updateIo.UpdateProfilePic(userId, url);

            return Ok(new { success });
        }
    }
}
