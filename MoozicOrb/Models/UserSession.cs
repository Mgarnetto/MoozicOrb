
namespace MoozicOrb.Models
{
    public class UserSession
    {
        public string SessionId { get; set; }
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
