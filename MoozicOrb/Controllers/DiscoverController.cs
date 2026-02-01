using Microsoft.AspNetCore.Mvc;
using MoozicOrb.Extensions;
using MoozicOrb.Models;

namespace MoozicOrb.Controllers
{
    public class DiscoverController : Controller
    {
        // URL: /genres
        //[HttpGet("genres")]
        //public IActionResult Genres()
        //{
        //    var model = new GenresViewModel
        //    {
        //        Genres = new List<GenreDto>
        //        {
        //            new GenreDto { Name = "Rock", Color = "#ff0055", IconClass = "fas fa-guitar" },
        //            new GenreDto { Name = "Electronic", Color = "#00AEEF", IconClass = "fas fa-bolt" },
        //            // ... fetch from DB
        //        }
        //    };

        //    if (Request.IsSpaRequest()) return PartialView("_GenresPartial", model);

        //    return View("Genres", model);
        //}
    }
}