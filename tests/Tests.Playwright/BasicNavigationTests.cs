using Microsoft.Playwright;
using System.Text.Json;

namespace Tests.Playwright
{
    [Collection("playwright")]
    public class BasicNavigationTests(PlaywrightFixture fixture)
    {
        [Fact]
        public async Task HomePageLoads()
        {
            await fixture.Page.GotoAsync(
                            $"{fixture.BaseUrl}/",
                            new() { 
                                WaitUntil = WaitUntilState.DOMContentLoaded,
                                Timeout = 60000 });
            var title = await fixture.Page.TitleAsync();
            var test = GetSiteName();
            Assert.Contains(GetSiteName(), title);
        }

        private static string GetSiteName()
        {
            var appsettingsPath = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..", "..", "..",
                "src", "OnigiriShop", "appsettings.json"));
            using var document = JsonDocument.Parse(File.ReadAllText(appsettingsPath));
            return document.RootElement.GetProperty("Site")
                           .GetProperty("Name").GetString()!;
        }
    }
}
