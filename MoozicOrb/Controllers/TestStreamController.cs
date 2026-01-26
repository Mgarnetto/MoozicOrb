using Microsoft.AspNetCore.Mvc;

namespace MoozicOrb.Controllers
{
    public class TestStreamController : Controller
    {
        [HttpGet("stream/test")]
        public IActionResult Test()
        {
            // Render the Razor view at Views/TestStream/Test.cshtml
            return View();
        }
    }
}

