using Microsoft.AspNetCore.Mvc;
using MoozicOrb.Extensions;
using MoozicOrb.Models;
using MoozicOrb.Services;

namespace MoozicOrb.Controllers
{
    //public class PageController : Controller
    //{
    //    private readonly IPostService _postService;

    //    public PageController(IPostService postService)
    //    {
    //        _postService = postService;
    //    }

    //    // URL: /articles
    //    [HttpGet("articles")]
    //    public async Task<IActionResult> Articles()
    //    {
    //        var model = new PageViewModel
    //        {
    //            Title = "Articles",
    //            SignalRGroup = "page_articles",
    //            Posts = await _postService.GetPostsForContextAsync("page", "articles")
    //        };

    //        if (Request.IsSpaRequest()) return PartialView("_ArticlesPartial", model);
    //        return View("Articles", model);
    //    }

    //    // URL: /classifieds
    //    [HttpGet("classifieds")]
    //    public async Task<IActionResult> Classifieds()
    //    {
    //        var model = new PageViewModel
    //        {
    //            Title = "Classifieds",
    //            SignalRGroup = "page_classifieds",
    //            Posts = await _postService.GetPostsForContextAsync("page", "classifieds")
    //        };

    //        if (Request.IsSpaRequest()) return PartialView("_ClassifiedsPartial", model);
    //        return View("Classifieds", model);
    //    }

    //    // URL: /tutorials
    //    [HttpGet("tutorials")]
    //    public async Task<IActionResult> Tutorials()
    //    {
    //        var model = new PageViewModel
    //        {
    //            Title = "Tutorials",
    //            SignalRGroup = "page_tutorials",
    //            Posts = await _postService.GetPostsForContextAsync("page", "tutorials")
    //        };

    //        if (Request.IsSpaRequest()) return PartialView("_TutorialsPartial", model);
    //        return View("Tutorials", model);
    //    }
    //}
}