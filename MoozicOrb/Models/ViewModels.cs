using MoozicOrb.API.Models;

namespace MoozicOrb.Models
{
    public class BaseViewModel
    {
        public string SignalRGroup { get; set; } // The critical piece for the Router
        public List<PostDto> Posts { get; set; }
    }

    public class LocationViewModel : BaseViewModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class CreatorViewModel : BaseViewModel
    {
        public int UserId { get; set; }
        public string DisplayName { get; set; }
        public string ProfilePicUrl { get; set; }
        public string Bio { get; set; }
    }
}
