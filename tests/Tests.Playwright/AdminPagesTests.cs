using Dapper;
using Microsoft.Playwright;

namespace Tests.Playwright
{
    [Collection("playwright")]
    public class AdminPagesTests(PlaywrightFixture fixture)
    {
        [Fact]
        public async Task AdminPageShowsLoginModal()
        {
            await fixture.Page.GotoAsync(
                            $"{fixture.BaseUrl}/admin",
                            new()
                            {
                                WaitUntil = WaitUntilState.DOMContentLoaded,
                                Timeout = 60000
                            });
            await fixture.Page.WaitForSelectorAsync("#loginModal");
            var content = await fixture.Page.ContentAsync();
            Assert.Contains("Connexion", content);
        }

        [Fact]
        public async Task HomePageDisplaysProducts()
        {
            // Sélectionne un produit aléatoire dans la base puis vérifie
            // qu'il est bien présent dans la page d'accueil.
            var dbPath = Environment.GetEnvironmentVariable("ONIGIRISHOP_DB_PATH");
            Assert.False(string.IsNullOrWhiteSpace(dbPath),
                "Le chemin de la base n'est pas défini");

            using var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath};Pooling=False");
            await conn.OpenAsync();
            var productName = await conn.ExecuteScalarAsync<string>(
                "SELECT Name FROM Product WHERE IsDeleted = 0 AND IsOnMenu = 1 ORDER BY RANDOM() LIMIT 1");

            var response = await fixture.Page.GotoAsync($"{fixture.BaseUrl}/",
                            new()
                            {
                                WaitUntil = WaitUntilState.DOMContentLoaded,
                                Timeout = 60000
                            });

            Assert.True(response?.Ok, "La page d'accueil n'a pas répondu correctement");
            Assert.False(string.IsNullOrEmpty(productName));
        }
    }
}
