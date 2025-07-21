using Microsoft.Playwright;

namespace Tests.Playwright
{
    public class PlaywrightFixture : IAsyncLifetime
    {
        public IPlaywright Playwright { get; private set; } = default!;
        public IBrowser Browser { get; private set; } = default!;
        public IPage Page { get; private set; } = default!;

        public async Task InitializeAsync()
        {
            Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            Page = await Browser.NewPageAsync();
        }

        public async Task DisposeAsync()
        {
            await Page.CloseAsync();
            await Browser.CloseAsync();
            Playwright.Dispose();
        }
    }

    [CollectionDefinition("playwright")]
    public class PlaywrightCollection : ICollectionFixture<PlaywrightFixture> { }
}
