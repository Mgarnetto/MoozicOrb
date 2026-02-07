using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MoozicOrb.API.Models;
using MoozicOrb.IO;
using MoozicOrb.Services; // Assuming SessionStore is here
using System.Collections.Generic;

namespace MoozicOrb.ViewComponents
{
    public class PostFeedViewComponent : ViewComponent
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PostFeedViewComponent(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public IViewComponentResult Invoke(string contextType, string contextId, bool allowPosting = true, string inputType = "standard")
        {
            // 1. Determine Viewer ID (So we know if they Liked posts)
            int viewerId = 0;
            var context = _httpContextAccessor.HttpContext;
            if (context != null && context.Request.Headers.TryGetValue("X-Session-Id", out var sessionId))
            {
                // Using your SessionStore helper safely
                var session = SessionStore.GetSession(sessionId.ToString());
                if (session != null) viewerId = session.UserId;
            }

            // 2. Fetch Data with Viewer Context
            var postIo = new GetPost();
            // Calls the NEW overload we created in GetPost.cs
            var posts = postIo.Execute(contextType, contextId, viewerId, 1);

            // 3. Build Model
            var model = new PostFeedViewModel
            {
                ContextType = contextType,
                ContextId = contextId,
                AllowPosting = allowPosting,
                InputType = inputType,
                InitialPosts = posts ?? new List<PostDto>(),
                ViewerId = viewerId 
            };

            return View(model);
        }
    }
}