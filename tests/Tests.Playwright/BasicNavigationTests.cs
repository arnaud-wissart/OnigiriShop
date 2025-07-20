using Microsoft.Playwright;

namespace Tests
{
    public class BasicNavigationTests : IAsyncLifetime
    {
        private IPlaywright _playwright;
        private IBrowser _browser;
        private IPage _page;

        public async Task InitializeAsync()
        {
            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            _page = await _browser.NewPageAsync();
        }

        public async Task DisposeAsync()
        {
            await _page.CloseAsync();
            await _browser.CloseAsync();
            _playwright.Dispose();
        }

        [Fact]
        public async Task HomePageLoads()
        {
            await _page.GotoAsync("http://localhost:5148/");
            var title = await _page.TitleAsync();
            Assert.Contains("Onigiri", title);
        }
    }
}