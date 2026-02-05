using Microsoft.AspNetCore.Mvc;
using MoozicOrb.API.Models;
using MoozicOrb.Extensions;
using MoozicOrb.IO;
using MoozicOrb.Models;
using MoozicOrb.Services; // For SessionStore
using System.Collections.Generic;

namespace MoozicOrb.Controllers
{
    [Route("discover")]
    public class DiscoverController : Controller
    {
        private readonly UserQuery _userQuery;
        private readonly GetPost _postQuery;

        public DiscoverController()
        {
            _userQuery = new UserQuery();
            _postQuery = new GetPost();
        }

        public IActionResult Index()
        {
            if (Request.IsSpaRequest()) return PartialView("_GenresPartial");
            return View();
        }

        [HttpGet("search")]
        public IActionResult Search(string q)
        {
            // 1. Get Viewer ID (optional, for 'isLiked' logic)
            string sid = Request.Headers["X-Session-Id"].ToString();
            var session = SessionStore.GetSession(sid);
            int viewerId = session?.UserId ?? 0;

            // 2. Perform Search
            var model = new SearchViewModel
            {
                Query = q,
                Users = new List<User>(),
                Posts = new List<PostDto>()
            };

            if (!string.IsNullOrWhiteSpace(q))
            {
                model.Users = _userQuery.SearchUsers(q);
                model.Posts = _postQuery.SearchPosts(q, viewerId);
            }

            // 3. Return View
            if (Request.IsSpaRequest())
            {
                return PartialView("_SearchPartial", model);
            }

            return View("Search", model);
        }
    }
}