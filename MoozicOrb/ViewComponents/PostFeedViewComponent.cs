using Microsoft.AspNetCore.Mvc;
using MoozicOrb.API.Models;
using MoozicOrb.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MoozicOrb.ViewComponents
{
    public class PostFeedViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(string contextType, string contextId, bool allowPosting = true, string inputType = "standard")
        {
            // 1. Fetch Data (Server-Side)
            // We fetch page 1 immediately so the user sees content instantly without a loading spinner.
            var postIo = new GetPost();
            var posts = postIo.Execute(contextType, contextId, 1);

            // 2. Build Model
            var model = new PostFeedViewModel
            {
                ContextType = contextType,
                ContextId = contextId,
                AllowPosting = allowPosting,
                InputType = inputType,
                InitialPosts = posts ?? new List<API.Models.PostDto>()
            };

            return View(model);
        }
    }
}