using Microsoft.AspNetCore.Mvc;
using MoozicOrb.IO;
using MoozicOrb.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MoozicOrb.ViewComponents
{
    public class DiscographyViewComponent : ViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync(int userId, bool isCurrentUser)
        {
            var collections = new List<CollectionDto>();

            // 1. Fetch the "All Uploads" Virtual Collection
            var audioIo = new GetUserAudioCollection();
            var masterCollection = audioIo.Execute(userId);

            if (masterCollection.Items.Count > 0)
            {
                collections.Add(masterCollection);
            }

            // 2. (Future) You can fetch other playlist collections here

            var model = new DiscographyViewModel
            {
                UserId = userId,
                IsCurrentUser = isCurrentUser,
                Collections = collections
            };

            return View(model);
        }
    }

    public class DiscographyViewModel
    {
        public int UserId { get; set; }
        public bool IsCurrentUser { get; set; }
        public List<CollectionDto> Collections { get; set; } = new();
    }
}