using Microsoft.AspNetCore.Mvc;
using MoozicOrb.IO;
using MoozicOrb.Models;
using MoozicOrb.Extensions;
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

        // ==========================================
        // NEW: "My Profile" Route
        // Handles navigation to /creator/profile without needing an ID in the URL
        // ==========================================
        [HttpGet("creator/profile")]
        public IActionResult MyProfile()
        {
            // 1. Resolve Identity from Server Session
            int? currentUserId = HttpContext.Session.GetInt32("UserId");

            // 2. Guard Clause: Not Logged In
            if (!currentUserId.HasValue || currentUserId.Value == 0)
            {
                // If this is an SPA fetch, returning 401 allows the frontend to trigger the login modal
                // Note: Ensure your IsSpaRequest() extension checks for "X-Spa-Request" header
                if (Request.IsSpaRequest() || Request.Headers["X-Spa-Request"] == "true")
                {
                    return Unauthorized();
                }

                // Otherwise, redirect to home
                return RedirectToAction("Index", "Home");
            }

            // 3. Pass through to the main Index method with the resolved ID
            return Index(currentUserId.Value);
        }

        // URL: /creator/105
        // Added :int constraint to prevent route conflict with "profile"
        [HttpGet("creator/{id:int}")]
        public IActionResult Index(int id)
        {
            // 1. Fetch Profile Data
            var user = _userQuery.GetUserById(id);
            if (user == null || user.UserId == 0) return NotFound();

            // 2. Determine if this is "Me"
            int currentUserId = HttpContext.Session.GetInt32("UserId") ?? 0;
            bool isMe = (currentUserId == id);

            // 3. Build Model
            var model = new CreatorViewModel
            {
                UserId = user.UserId,
                DisplayName = user.DisplayName,
                UserName = user.UserName,
                ProfilePic = user.ProfilePic,
                CoverImage = user.CoverImageUrl,
                Bio = user.Bio,
                IsCurrentUser = isMe,
                LayoutOrder = user.LayoutOrder,
                SignalRGroup = $"user_{user.UserId}"
            };

            // 4. Return View (SPA Partial or Full Layout)
            // Checks custom Extension, specific Router header, or query param
            if (Request.IsSpaRequest() ||
                Request.Headers["X-Spa-Request"] == "true" ||
                Request.Query["spa"] == "1")
            {
                return PartialView("_ProfilePartial", model);
            }

            return View("Index", model);
        }
    }
}
