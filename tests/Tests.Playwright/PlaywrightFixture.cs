using Microsoft.Playwright;
using System.Diagnostics;

namespace Tests.Playwright
{
    public class PlaywrightFixture : IAsyncLifetime
    {
        public IPlaywright Playwright { get; private set; } = default!;
        public IBrowser Browser { get; private set; } = default!;
        public IPage Page { get; private set; } = default!;
        private Process? _appProcess;
        public string BaseUrl { get; } = "http://localhost:5148";
        public async Task InitializeAsync()
        {
            var projectPath = Path.GetFullPath(Path.Combine("..", "..", "src", "OnigiriShop", "OnigiriShop.csproj"));
            var startInfo = new ProcessStartInfo("dotnet", $"run --project \"{projectPath}\" --urls {BaseUrl}")
            {
                WorkingDirectory = Path.GetDirectoryName(projectPath)!,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                Environment = { ["ASPNETCORE_ENVIRONMENT"] = "Development" }
            };

            _appProcess = Process.Start(startInfo);

            using var client = new HttpClient();
            for (int i = 0; i < 30; i++)
            {
                await Task.Delay(1000);
                try
                {
                    var response = await client.GetAsync(BaseUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        break;
                    }
                }
                catch
                {
                    // ignore while waiting for the server
                }
            }

            Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            Page = await Browser.NewPageAsync();
        }

        public async Task DisposeAsync()
        {
            await Page.CloseAsync();
            await Browser.CloseAsync();
            Playwright.Dispose();

            if (_appProcess is { HasExited: false })
            {
                _appProcess.Kill(entireProcessTree: true);
                await _appProcess.WaitForExitAsync();
            }
            _appProcess?.Dispose();
        }
    }

    [CollectionDefinition("playwright")]
    public class PlaywrightCollection : ICollectionFixture<PlaywrightFixture> { }
}
