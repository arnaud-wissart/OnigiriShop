using Microsoft.Playwright;

namespace Tests.Playwright
{
    [Collection("playwright")]
    public class AdminPagesTests(PlaywrightFixture fixture)
    {
        [Fact]
        public async Task AdminPageShowsLoginModal()
        {
            await fixture.Page.GotoAsync("http://localhost:5148/admin");
            await fixture.Page.WaitForSelectorAsync("text=Connexion");
            var content = await fixture.Page.ContentAsync();
            Assert.Contains("Connexion", content);
        }

        [Fact]
        public async Task HomePageDisplaysProducts()
        {
            await fixture.Page.GotoAsync("http://localhost:5148/");
            await fixture.Page.WaitForSelectorAsync(".onigiri-card");
            var content = await fixture.Page.ContentAsync();
            Assert.Contains("Onigiri Saumon", content);
        }
    }
}
