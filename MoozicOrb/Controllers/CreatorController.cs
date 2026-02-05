using Microsoft.AspNetCore.Mvc;
using MoozicOrb.IO;
using MoozicOrb.Models;
using MoozicOrb.Services; // CORRECT NAMESPACE for SessionStore
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
        // 1. "My Profile" Route (/creator/profile)
        // ==========================================
        [HttpGet("creator/profile")]
        public IActionResult MyProfile()
        {
            // 1. Resolve Identity using the STATIC SessionStore
            string sid = Request.Headers["X-Session-Id"].ToString();

            // Fix: Use the Services namespace, not IO
            var session = SessionStore.GetSession(sid);

            int currentUserId = session?.UserId ?? 0;

            // 2. Guard Clause: Not Logged In
            if (currentUserId == 0)
            {
                // If SPA, return 401 to trigger login modal
                if (Request.IsSpaRequest() || Request.Headers["X-Spa-Request"] == "true")
                {
                    return Unauthorized();
                }
                // If direct browser hit, go home
                return RedirectToAction("Index", "Home");
            }

            // 3. Pass through to the main Index method
            return Index(currentUserId);
        }

        // ==========================================
        // 2. Specific User Route (/creator/105)
        // ==========================================
        [HttpGet("creator/{id:int}")]
        public IActionResult Index(int id)
        {
            // 1. Fetch Profile Data
            var user = _userQuery.GetUserById(id);
            if (user == null || user.UserId == 0) return NotFound();

            // 2. Determine if this is "Me"
            string sid = Request.Headers["X-Session-Id"].ToString();
            var session = SessionStore.GetSession(sid);
            int currentUserId = session?.UserId ?? 0;

            bool isMe = (currentUserId == id);

            // 3. Build Model
            var model = new CreatorViewModel
            {
                UserId = user.UserId,
                DisplayName = user.DisplayName ?? user.UserName,
                UserName = user.UserName,
                ProfilePic = user.ProfilePic,
                CoverImage = user.CoverImageUrl,
                Bio = user.Bio,
                IsCurrentUser = isMe,
                LayoutOrder = user.LayoutOrder ?? new List<string>(),
                SignalRGroup = $"user_{user.UserId}",
                Collections = new List<CollectionDto>()
            };

            // 4. Return View
            if (Request.IsSpaRequest() || Request.Headers["X-Spa-Request"] == "true")
            {
                return PartialView("_ProfilePartial", model);
            }

            return View("Index", model);
        }

        // ==========================================
        // 3. Sidebar Info Endpoint (For the next step)
        // ==========================================
        [HttpGet("api/creator/sidebar-info")]
        public IActionResult GetSidebarInfo()
        {
            string sid = Request.Headers["X-Session-Id"].ToString();
            var session = SessionStore.GetSession(sid);
            int currentUserId = session?.UserId ?? 0;

            if (currentUserId == 0) return Unauthorized();

            var user = _userQuery.GetUserById(currentUserId);
            if (user == null) return NotFound();

            return Ok(new
            {
                name = user.DisplayName ?? user.UserName,
                pic = user.ProfilePic ?? "/img/profile_default.jpg",
                followers = 0,
                following = 0
            });
        }
    }
}
