using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using MoozicOrb.Extensions;
using MoozicOrb.Models;
using MoozicOrb.Services;
using MoozicOrb.Services.Interfaces;

namespace MoozicOrb.Controllers
{
    public class CreatorController : Controller
    {
        //<a href = "/creator/105" > View Profile</a>
        //<a href = "/creator/@post.AuthorId" > @post.AuthorName </ a >

        //private readonly IUserService _userService;
        //private readonly IPostService _postService;

        //public CreatorController(IUserService userService, IPostService postService)
        //{
        //    _userService = userService;
        //    _postService = postService;
        //}

        //// URL: /creator/105
        //[HttpGet("creator/{id}")]
        //public async Task<IActionResult> Index(int id)
        //{
        //    var user = await _userService.GetUserByIdAsync(id);
        //    if (user == null) return NotFound();

        //    var model = new CreatorViewModel
        //    {
        //        UserId = user.Id,
        //        DisplayName = user.DisplayName,
        //        ProfilePicUrl = user.ProfilePicUrl,
        //        Bio = user.Bio,
        //        // CONVENTION: "user_" + DB ID
        //        SignalRGroup = $"user_{user.Id}",
        //        Posts = await _postService.GetPostsForContextAsync("user", user.Id)
        //    };

        //    if (Request.IsSpaRequest())
        //    {
        //        return PartialView("_CreatorPartial", model);
        //    }

        //    return View("Index", model);
        //}
    }
}
