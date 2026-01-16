using Microsoft.AspNetCore.Mvc;
using MoozicOrb.IO;
using MoozicOrb.Models;
using System.Diagnostics;

namespace MoozicOrb.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

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
            return View();
        }

        public IActionResult Mainpage()
        {
            return View();
        }
        public IActionResult Test()
        {
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
