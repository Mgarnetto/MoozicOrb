using Microsoft.AspNetCore.Mvc;
using MoozicOrb.Extensions; // For IsSpaRequest()
using MoozicOrb.Models;
using MoozicOrb.Services; // Assuming you have a service/db

namespace MoozicOrb.Controllers
{
    public class LocationController : Controller
    {
        //<a href = "/discover/us-ga" > Georgia </ a >
        //< a href="/discover/fr-paris">Paris</a>

        //private readonly ILocationService _locationService;
        //private readonly IPostService _postService;

        //public LocationController(ILocationService locationService, IPostService postService)
        //{
        //    _locationService = locationService;
        //    _postService = postService;
        //}

        //// URL: /discover/us-ga
        //[HttpGet("discover/{slug}")]
        //public async Task<IActionResult> Index(string slug)
        //{
        //    // 1. Get Location from DB
        //    var location = await _locationService.GetBySlugAsync(slug);
        //    if (location == null) return NotFound();

        //    // 2. Build ViewModel
        //    var model = new LocationViewModel
        //    {
        //        Name = location.DisplayName,
        //        Description = location.Description,
        //        // CONVENTION: "loc_" + DB ID
        //        SignalRGroup = $"loc_{location.Id}",
        //        // Get posts specific to this location
        //        Posts = await _postService.GetPostsForContextAsync("loc", location.Id)
        //    };

        //    // 3. The SPA Switch
        //    if (Request.IsSpaRequest())
        //    {
        //        // Returns only the HTML for the middle of the page
        //        return PartialView("_LocationPartial", model);
        //    }

        //    // Returns Full Page (Layout + Partial)
        //    return View("Index", model);
        //}
    }
}