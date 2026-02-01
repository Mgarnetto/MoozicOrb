using Microsoft.AspNetCore.Mvc;
using MoozicOrb.Extensions;
using MoozicOrb.Models;
using MoozicOrb.Services;
using MoozicOrb.Services.Interfaces;

namespace MoozicOrb.Controllers
{
    [Route("settings")]
    public class SettingsController : Controller
    {
        private readonly IUserService _userService;

        public SettingsController(IUserService userService)
        {
            _userService = userService;
        }

        // URL: /settings/account
        //[HttpGet("account")]
        //public async Task<IActionResult> Account()
        //{
        //    // TODO: Get actual User ID from session
        //    int userId = 1;
        //    var user = await _userService.GetUserByIdAsync(userId);

        //    var model = new AccountSettingsViewModel
        //    {
        //        DisplayName = user.DisplayName,
        //        Email = user.Email
        //    };

        //    if (Request.IsSpaRequest()) return PartialView("_AccountSettingsPartial", model);
        //    return View("Account", model);
        //}

        // URL: /settings/page
        [HttpGet("page")]
        public async Task<IActionResult> Page()
        {
            int userId = 1;
            //var user = await _userService.GetUserByIdAsync(userId);

            var model = new PageSettingsViewModel
            {
                //Bio = user.Bio,
                //CoverImage = user.CoverImageUrl,
                //BookingEmail = user.Email
            };

            if (Request.IsSpaRequest()) return PartialView("_PageSettingsPartial", model);
            return View("Page", model);
        }
    }
}
