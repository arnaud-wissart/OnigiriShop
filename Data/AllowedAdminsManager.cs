using System.Text.Json;

namespace OnigiriShop.Data
{
    public class AllowedAdminsManager
    {
        private readonly string _jsonPath;
        private List<AdminCredential> _admins;

        public AllowedAdminsManager(string jsonPath)
        {
            _jsonPath = jsonPath;
            Load();
        }

        private void Load()
        {
            if (!File.Exists(_jsonPath))
                _admins = [];
            else
            {
                var json = File.ReadAllText(_jsonPath);
                var obj = JsonSerializer.Deserialize<AdminList>(json);
                _admins = obj?.Admins ?? [];
            }
        }

        public bool Validate(string email, string password)
        {
            return _admins.Any(a =>
                string.Equals(a.Email, email, StringComparison.OrdinalIgnoreCase) &&
                a.Password == password);
        }

        public class AdminCredential
        {
            public string Email { get; set; }
            public string Password { get; set; }
        }
        private class AdminList
        {
            public List<AdminCredential> Admins { get; set; }
        }
    }
}
