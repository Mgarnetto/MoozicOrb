using Microsoft.AspNetCore.Mvc;
using MoozicOrb.API.Models;
using MoozicOrb.Extensions;
using MoozicOrb.IO;
using MoozicOrb.Models;
using MoozicOrb.Services;
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
            // 1. Get Viewer ID 
            string sid = Request.Headers["X-Session-Id"].ToString();
            var session = SessionStore.GetSession(sid);
            int viewerId = session?.UserId ?? 0;

            var model = new SearchViewModel
            {
                Query = q,
                Users = new List<User>(),
                Posts = new List<PostDto>()
            };

            if (!string.IsNullOrWhiteSpace(q))
            {
                // Keep searching users as normal
                model.Users = _userQuery.SearchUsers(q);

                // FIX: Use SearchAudio to strictly return music tracks
                model.Posts = _postQuery.SearchAudio(q, viewerId);
            }

            if (Request.IsSpaRequest())
            {
                return PartialView("_SearchPartial", model);
            }

            return View("Search", model);
        }
    }
}