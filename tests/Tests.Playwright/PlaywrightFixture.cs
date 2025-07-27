using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using OnigiriShop.Infrastructure;
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

            // Prépare une base dédiée pour les tests et exécute les migrations
            var dbPath = Path.Combine(Path.GetTempPath(), $"onigiri_{Guid.NewGuid()}.db");
            Environment.SetEnvironmentVariable("ONIGIRISHOP_DB_PATH", dbPath);
            var services = new ServiceCollection()
                .AddOnigiriMigrations($"Data Source={dbPath};Pooling=False");

            services.AddLogging(lb => lb.AddFluentMigratorConsole());
            using (var sp = services.BuildServiceProvider())
            {
                var runner = sp.GetRequiredService<IMigrationRunner>();
                runner.MigrateUp();
            }
            var projectPath = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory, "..", "..", "..", "..", "..",
                "src", "OnigiriShop", "OnigiriShop.csproj"));
            var projectDir = Path.GetDirectoryName(projectPath)!;
            var buildOutput = Path.Combine(projectDir, "bin", "Release", "net8.0", "OnigiriShop.dll");
            if (!File.Exists(buildOutput))
            {
                var buildInfo = new ProcessStartInfo("dotnet", $"build \"{projectPath}\" -c Release")
                {
                    WorkingDirectory = projectDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                };
                var buildProcess = Process.Start(buildInfo)!;
                buildProcess.OutputDataReceived += (_, e) => { if (e.Data != null) Console.WriteLine(e.Data); };
                buildProcess.ErrorDataReceived += (_, e) => { if (e.Data != null) Console.WriteLine(e.Data); };
                buildProcess.BeginOutputReadLine();
                buildProcess.BeginErrorReadLine();
                await buildProcess.WaitForExitAsync();
                if (buildProcess.ExitCode != 0)
                    throw new InvalidOperationException("La construction du projet a échoué.");
            }
            var args = $"run --no-build --configuration Release --project \"{projectPath}\" --urls {BaseUrl}";
            var startInfo = new ProcessStartInfo("dotnet", args)
            {
                WorkingDirectory = projectDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
            startInfo.Environment["ASPNETCORE_HTTPS_PORT"] = "0";

            _appProcess = Process.Start(startInfo)!;
            _appProcess.OutputDataReceived += (_, e) => { if (e.Data != null) Console.WriteLine(e.Data); };
            _appProcess.ErrorDataReceived += (_, e) => { if (e.Data != null) Console.WriteLine(e.Data); };
            _appProcess.BeginOutputReadLine();
            _appProcess.BeginErrorReadLine();

            using var client = new HttpClient();
            var started = false;
            for (int i = 0; i < 10; i++)
            {
                await Task.Delay(1000);
                try
                {
                    var response = await client.GetAsync(BaseUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        started = true;
                        break;
                    }                        
                }
                catch
                {
                    // ignore while waiting for the server
                }
            }
            if (!started)
                throw new InvalidOperationException("Le serveur de test ne s'est pas lancé.");

            Program.Main(["install", "chromium", "--with-deps"]);

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

            var dbPath = Environment.GetEnvironmentVariable("ONIGIRISHOP_DB_PATH");
            if (!string.IsNullOrWhiteSpace(dbPath) && File.Exists(dbPath))
                File.Delete(dbPath);
            Environment.SetEnvironmentVariable("ONIGIRISHOP_DB_PATH", null);
        }
    }

    [CollectionDefinition("playwright")]
    public class PlaywrightCollection : ICollectionFixture<PlaywrightFixture> { }
}
