using Microsoft.AspNetCore.Mvc;
using MoozicOrb.IO;
using MoozicOrb.Models;
using MoozicOrb.Extensions; // Ensure you have this for IsSpaRequest()
using System.Collections.Generic;

namespace MoozicOrb.Controllers
{
    public class CreatorController : Controller
    {
        private readonly UserQuery _userQuery;

        public CreatorController()
        {
            _userQuery = new UserQuery();
        }

        // URL: /creator/105
        [HttpGet("creator/{id}")]
        public IActionResult Index(int id)
        {
            // 1. Fetch Profile Data
            var user = _userQuery.GetUserById(id);
            if (user == null || user.UserId == 0) return NotFound();

            // 2. Determine if this is "Me"
            // (Assumes you store the logged-in UserId in Session as an int)
            int currentUserId = HttpContext.Session.GetInt32("UserId") ?? 0;
            bool isMe = (currentUserId == id);

            // 3. Build Model
            var model = new CreatorViewModel
            {
                UserId = user.UserId,
                DisplayName = user.DisplayName,
                UserName = user.UserName,
                ProfilePic = user.ProfilePic, // Now using the direct string from DB
                CoverImage = user.CoverImageUrl,
                Bio = user.Bio,
                IsCurrentUser = isMe,
                LayoutOrder = user.LayoutOrder, // Uses the JSON parser we added to User.cs
                SignalRGroup = $"user_{user.UserId}"
            };

            // 4. Return View
            // If SPA (AJAX navigation), return just the partial. Otherwise, full layout.
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Query["spa"] == "1")
            {
                return PartialView("_ProfilePartial", model);
            }

            return View("Index", model);
        }
    }
}
