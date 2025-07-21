using Microsoft.Playwright;

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
            Assert.Contains("Onigiri", title);
        }
    }
}
