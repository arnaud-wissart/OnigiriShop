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
                            new() { 
                                WaitUntil = WaitUntilState.DOMContentLoaded,
                                Timeout = 60000 });
            await fixture.Page.WaitForSelectorAsync("#loginModal");
            var content = await fixture.Page.ContentAsync();
            Assert.Contains("Connexion", content);
        }

        [Fact]
        public async Task HomePageDisplaysProducts()
        {
            await fixture.Page.GotoAsync(
                            $"{fixture.BaseUrl}/",
                            new() { 
                                WaitUntil = WaitUntilState.DOMContentLoaded,
                                Timeout = 60000 });
            await fixture.Page.WaitForSelectorAsync(".onigiri-card");
            var content = await fixture.Page.ContentAsync();
            Assert.Contains("Onigiri Saumon", content);
        }
    }
}
