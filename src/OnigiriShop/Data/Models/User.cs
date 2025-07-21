namespace OnigiriShop.Data.Models
{
    public class User
    {
        public int Id { get; set; }
        public string? Email { get; set; }
        public string? Name { get; set; }
        public string? Phone { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public string? Role { get; set; }

        public string? PasswordHash { get; set; }
        public string? PasswordSalt { get; set; }
    }
}