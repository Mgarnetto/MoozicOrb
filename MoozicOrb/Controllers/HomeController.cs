using Microsoft.AspNetCore.Mvc;
using MoozicOrb.Extensions;
using MoozicOrb.IO;
using MoozicOrb.Models;
using System.Diagnostics;

namespace MoozicOrb.Controllers
{
    public class HomeController : Controller
    { 
        public IActionResult Index()
        {
            //string fail = "success";

            //try
            //{
            //        var insertUser = new InsertUser();

            //        fail = "step1";

            //    var user = new MoozicOrb.Models.User()
            //        {
            //            FirstName = "Marcus",
            //            MiddleName = "",
            //            LastName = "Garnetto",
            //            UserName = "marcus",
            //            ProfilePic = "",
            //            UserGroups = "9",
            //            IsArtist = true
            //        };

            //        long userId = insertUser.Insert(user);

            //        if (userId <= 0)
            //        {
            //            int failed = 1;
            //        }

            //        // ---- STEP 2: hash password ----
            //        string password = "password";

            //     fail = "step2";

            //    string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

            //        // ---- STEP 3: insert auth row ----
            //        var insertAuth = new InsertUserAuthLocal();
            //        long authId = insertAuth.Insert(userId, passwordHash);
            //    fail = "step3";
            //    if (authId <= 0)
            //        {
            //            int failed = 1;
            //        }
            //}catch(Exception ex)
            //{
            //    fail = "error"; 
            //    ViewBag.Error = fail;
            //    return View();
            //}
            //fail = "completed";
            //ViewBag.Error = fail;

            if (Request.IsSpaRequest())
            {
                return PartialView("_HomePartial");
            }
            return View();
        }

        //public class HomeController : Controller
        //{
        //    private readonly IPostService _postService;

        //    public HomeController(IPostService postService)
        //    {
        //        _postService = postService;
        //    }

        //    // URL: / (The Social Feed)
        //    public async Task<IActionResult> Index()
        //    {
        //        // TODO: Get current user ID to customize feed
        //        var model = new HomeViewModel
        //        {
        //            // CONVENTION: "feed_global" or "feed_user_{id}"
        //            SignalRGroup = "feed_global",
        //            Posts = await _postService.GetGlobalFeedAsync()
        //        };

        //        if (Request.IsSpaRequest()) return PartialView("_SocialFeedPartial", model);

        //        // On refresh, load Layout + Partial
        //        return View("Index", model);
        //    }
        //}
    }
}
