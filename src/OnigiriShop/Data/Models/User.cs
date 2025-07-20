namespace OnigiriShop.Data.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;          // Obligatoire, unique
        public string? Name { get; set; }           // Affiché, fallback sur Email si vide
        public string? Phone { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public string Role { get; set; } = string.Empty;           // "User", "Admin", etc.

        // Auth fields (non exposés côté client)
        public string? PasswordHash { get; set; }   // Null tant que le user n’a pas activé son compte
        public string? PasswordSalt { get; set; }   // Null tant que le user n’a pas activé son compte
    }

}
