using Microsoft.AspNetCore.Mvc;
using MoozicOrb.Extensions;
using MoozicOrb.IO;
using MoozicOrb.Models;
using System.Diagnostics;

namespace MoozicOrb.Controllers
{
    public class HomeController : Controller
    {
        // URL: /  (Stays as your landing/home page)
        public IActionResult Index()
        {


            //try
            //{
            //    var insertUser = new InsertUser();



            //    var user = new MoozicOrb.Models.User()
            //    {
            //        FirstName = "Kayla",
            //        MiddleName = "",
            //        LastName = "Garnetto",
            //        UserName = "KK",
            //        Email = "kayla@placeholder.com",
            //        DisplayName = "KK",
            //        ProfilePic = "",
            //        CoverImageUrl = "",
            //        Bio = "KK's Edibles",
            //        IsCreator = true,
            //        ProfileLayoutJson = "['posts', 'music', 'store']",
            //        UserGroups = "9"
            //    };

            //    long userId = insertUser.Execute(user);

            //    if (userId <= 0)
            //    {
            //        int failed = 1;
            //    }

            //    // ---- STEP 2: hash password ----
            //    string password = "password";



            //    string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

            //    // ---- STEP 3: insert auth row ----
            //    var insertAuth = new InsertUserAuthLocal();
            //    long authId = insertAuth.Insert(userId, passwordHash);

            //    if (authId <= 0)
            //    {
            //        int failed = 1;
            //    }
            //}
            //catch (Exception ex)
            //{

            //    return View();
            //}


            if (Request.IsSpaRequest())
            {
                return PartialView("_HomePartial");
            }
            return View();
        }

        // URL: /feed (The NEW Social Feed Page)
        [Route("feed")]
        public IActionResult Feed()
        {
            if (Request.IsSpaRequest())
            {
                return PartialView("_SocialFeedPartial");
            }
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
