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
        public string BaseUrl { get; private set; } = default!;

        private static int GetFreePort()
        {
            var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            listener.Start();
            var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
        public async Task InitializeAsync()
        {
            var port = GetFreePort();
            BaseUrl = $"http://localhost:{port}";
            var projectPath = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory, "..", "..", "..", "..", "..",
                "src", "OnigiriShop", "OnigiriShop.csproj"));
            var startInfo = new ProcessStartInfo("dotnet", $"run --project \"{projectPath}\" --urls {BaseUrl}")
            {
                WorkingDirectory = Path.GetDirectoryName(projectPath)!,
                RedirectStandardOutput = true,
                RedirectStandardError = true            
            };
            startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
            startInfo.Environment["ASPNETCORE_HTTPS_PORT"] = "0";
            _appProcess = Process.Start(startInfo);

            using var client = new HttpClient();
            for (int i = 0; i < 90; i++)
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
            if (Page is not null)
                await Page.CloseAsync();
            if (Browser is not null)
                await Browser.CloseAsync();
            Playwright?.Dispose();

            if (_appProcess != null)
            {
                if (!_appProcess.HasExited)
                {
                    _appProcess.Kill(entireProcessTree: true);
                    await _appProcess.WaitForExitAsync();
                }
                _appProcess.Dispose();
            }
        }
    }

    [CollectionDefinition("playwright")]
    public class PlaywrightCollection : ICollectionFixture<PlaywrightFixture> { }
}
