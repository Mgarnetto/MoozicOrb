using Microsoft.AspNetCore.Mvc;
using MoozicOrb.Extensions;
using MoozicOrb.Models;

namespace MoozicOrb.Controllers
{
    public class RadioController : Controller
    {
        //URL: /radio
        [HttpGet("radio")]
        public IActionResult Index()
        {
            var model = new RadioViewModel(); // Add properties if needed

            if (Request.IsSpaRequest()) return PartialView("_RadioOrbPartial", model);

            return RedirectToAction("Index", "Home");
        }
    }
}