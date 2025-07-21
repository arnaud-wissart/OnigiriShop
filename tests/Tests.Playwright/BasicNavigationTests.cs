namespace Tests.Playwright
{
    [Collection("playwright")]
    public class BasicNavigationTests(PlaywrightFixture fixture)
    {
        [Fact]
        public async Task HomePageLoads()
        {
            await fixture.Page.GotoAsync($"{fixture.BaseUrl}/");
            var title = await fixture.Page.TitleAsync();
            Assert.Contains("Onigiri", title);
        }
    }
}