namespace MoozicOrb.Models
{
    public class UserAuthLocal
    {
        public int UserId { get; set; }
        public string PasswordHash { get; set; }
        public string Salt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
