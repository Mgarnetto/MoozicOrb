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
