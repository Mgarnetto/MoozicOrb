using Microsoft.AspNetCore.Mvc;

namespace MoozicOrb.Controllers
{
    public class TestStreamController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/TestStream/Test.cshtml");
        }
    }
}