namespace Tests.Playwright
{
    [Collection("playwright")]
    public class BasicNavigationTests(PlaywrightFixture fixture)
    {
        [Fact]
        public async Task HomePageLoads()
        {
            await fixture.Page.GotoAsync("http://localhost:5148/");
            var title = await fixture.Page.TitleAsync();
            Assert.Contains("Onigiri", title);
        }
    }
}