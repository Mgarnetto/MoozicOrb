using Microsoft.AspNetCore.Mvc;
using MoozicOrb.API.Models;
using MoozicOrb.IO;
using MoozicOrb.API.Models; // Ensure this namespace matches your ViewModel location
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MoozicOrb.ViewComponents
{
    public class PostFeedViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(string contextType, string contextId, bool allowPosting = true, string inputType = "standard")
        {
            // 1. Server-Side Data Fetch
            // We get the first page of posts immediately.
            // "GetPost" is the IO class you already have.
            var postIo = new GetPost();
            var posts = postIo.Execute(contextType, contextId, 1);

            // 2. Build the Configuration Model
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