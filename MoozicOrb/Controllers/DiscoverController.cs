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
            var model = new GenresViewModel
            {
                Genres = new List<GenreItem>
                {
                    new GenreItem { Name = "Hip Hop", Color = "#ff0055", IconClass = "fas fa-microphone-alt" },
                    new GenreItem { Name = "Electronic", Color = "#00AEEF", IconClass = "fas fa-bolt" },
                    new GenreItem { Name = "Rock", Color = "#ffcc00", IconClass = "fas fa-guitar" },
                    new GenreItem { Name = "Jazz", Color = "#9900ff", IconClass = "fas fa-sax-hot" },
                    new GenreItem { Name = "R&B", Color = "#00ff99", IconClass = "fas fa-heart" },
                    new GenreItem { Name = "Classical", Color = "#ff6600", IconClass = "fas fa-music" }
                }
            };

            if (Request.IsSpaRequest()) return PartialView("_GenresPartial", model);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet("audio")]
        public IActionResult AudioFeed()
        {
            // This returns the empty wrapper which will load the JS to fetch JSON data
            // Consistent with the 'Playlist' visual style requested
            if (Request.IsSpaRequest()) return PartialView("_AudioPartial");
            return RedirectToAction("Index", "Home");
        }

        [HttpGet("search")]
        public IActionResult Search(string q)
        {
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
                model.Users = _userQuery.SearchUsers(q);
                // STRICT AUDIO SEARCH
                model.Posts = _postQuery.SearchPosts(q, viewerId);
            }

            if (Request.IsSpaRequest()) return PartialView("_SearchPartial", model);
            return RedirectToAction("Index", "Home");
        }
    }

    public class GenresViewModel
    {
        public List<GenreItem> Genres { get; set; } = new List<GenreItem>();
    }

    public class GenreItem
    {
        public string Name { get; set; }
        public string Color { get; set; }
        public string IconClass { get; set; }
    }
}